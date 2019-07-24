using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Itinero.Profiles;
using Itinero.Transit.Data;
using Itinero.Profiles.Lua.Osm;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.OtherMode;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Itinero.Profiles.Lua;
using System.IO;
using Itinero.Data.Graphs.Coders;
using Itinero.IO.Osm.Tiles;
using Itinero.Transit.Data.Core;
using Serilog;


namespace Itinero.Transit.Api.Logic
{
    public class OtherModeBuilder
    {
        public readonly RouterDb RouterDb;

        public readonly
            Dictionary<string,
                Func<Dictionary<string, string>, List<StopId>, List<StopId>, (IOtherModeGenerator, bool useCache)>>
            Factories =
                new Dictionary<string, Func<Dictionary<string, string>, List<StopId>, List<StopId>, (IOtherModeGenerator
                    , bool useCache)>>();


        // First profile is the default profile
        public readonly List<Profile> OsmVehicleProfiles = new List<Profile>
        {
            OsmProfiles.Pedestrian,
            OsmProfiles.Bicycle
        };

        public OtherModeBuilder(
            string osmRoutableTilesCacheDirectory = null,
            IConfiguration configuration = null)
        {
            if (!string.IsNullOrEmpty(osmRoutableTilesCacheDirectory))
            {
                OsmTransferGenerator.EnableCaching(osmRoutableTilesCacheDirectory);
            }

            var edgeDataLayouts = new List<(string key, EdgeDataType dataType)>();


            AddProfilesFromConfig(configuration);

            foreach (var profile in OsmVehicleProfiles)
            {
                edgeDataLayouts.Add((profile.Name + ".weight", EdgeDataType.UInt32));
            }

            // initialize the routerdb with a configuration to cache profile weights.
            // REMARK: this explicit router db layouting will be removed later.
            RouterDb = new RouterDb(new RouterDbConfiguration()
            {
                Zoom = 14,
                EdgeDataLayout = new EdgeDataLayout(edgeDataLayouts)
            });
            RouterDb.DataProvider = new DataProvider(RouterDb);

            AddFactories();
        }

        private void AddProfilesFromConfig(IConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            foreach (var path in configuration.GetChildren())
            {
                try
                {
                    var profile = LuaProfile.Load(File.ReadAllText(path.GetValue<string>("path")));
                    OsmVehicleProfiles.Add(profile);
                }
                catch (Exception e)
                {
                    Log.Error($"Could not load the OSM-Profile " + e);
                }
            }
        }

        private void AddFactories()
        {
            Factories.Add(
                new CrowsFlightTransferGenerator().FixedId(),
                (dict, __, _) =>
                {
                    var gen = new CrowsFlightTransferGenerator(
                        (uint) dict.Value("maxDistance", 500),
                        dict.Value("speed", 1.4f)
                    );
                    return (gen, false);
                });

            Factories.Add(
                // THis is only a dummy OsmTransferGenerator, used to get the identifying string
                // So we pass in a dummy routerdb which is not loaded
                new OsmTransferGenerator(RouterDb).FixedId(),
                (dict, _, __) =>
                {
                    var profileName = dict.Value("profile", "pedestrian");
                    var profile = GetOsmProfile(profileName);
                    if (profile == null)
                    {
                        throw new KeyNotFoundException($"The profile {profileName} was not found");
                    }

                    var gen = new OsmTransferGenerator(RouterDb,
                        (uint) dict.Value("maxDistance", 500),
                        profile
                    );

                    return (gen, true);
                });

            Factories.Add(
                new InternalTransferGenerator().FixedId(),
                (dict, _, __) =>
                {
                    var gen = new InternalTransferGenerator(dict.Value<uint>("timeNeeded", 180));
                    return (gen, false);
                });


            Factories.Add(
                new FirstLastMilePolicy(
                    new DummyOtherMode(), new DummyOtherMode(), new List<StopId>(),
                    new DummyOtherMode(), new List<StopId>()).FixedId(),
                (dict, departures, arrivals) =>
                {
                    var defaultModeString =
                        new OsmTransferGenerator(RouterDb).OtherModeIdentifier();
                    var defaultMode = Uri.UnescapeDataString(dict.Value("default", defaultModeString));
                    var firstMile = Uri.UnescapeDataString(dict.Value("firstMile", defaultModeString));
                    var lastMile = Uri.UnescapeDataString(dict.Value("lastMile", defaultModeString));

                    var gen = new FirstLastMilePolicy(
                        Create(defaultMode, departures, arrivals),
                        Create(firstMile, departures, arrivals),
                        departures,
                        Create(lastMile, departures, arrivals),
                        arrivals
                    );
                    return (gen, false);
                }
            );
        }

        public List<string> SupportedUrls()
        {
            var urls = new List<string>();

            foreach (var f in Factories)
            {
                var mode = f.Value.Invoke(new Dictionary<string, string>(), new List<StopId>(), new List<StopId>());
                urls.Add(mode.Item1.OtherModeIdentifier());
            }

            return urls;
        }

        private readonly Dictionary<string, IOtherModeGenerator> _cachedOtherModeGenerators =
            new Dictionary<string, IOtherModeGenerator>();


        public IOtherModeGenerator Create(string description, List<StopId> starts, List<StopId> ends)
        {
            if (_cachedOtherModeGenerators.ContainsKey(description))
            {
                return _cachedOtherModeGenerators[description];
            }

            var fixedPart = description.Split("&")[0];
            if (!Factories.ContainsKey(fixedPart))
            {
                throw new KeyNotFoundException("The profile could not be decoded: " + description);
            }

            var dict = ParseUriSettings(description);
            var (walkGen, useCache) = Factories[fixedPart](dict, starts, ends);
            foreach (var kv in dict)
            {
                throw new ArgumentException(
                    $"Wrong parameter in {description}: the following key was not used {kv.Key}");
            }

            if (useCache)
            {
                walkGen = walkGen.UseCache();
                _cachedOtherModeGenerators[description] = walkGen;
            }

            return walkGen;
        }


        private static readonly Regex _regex = new Regex(@"[?|&]([\w\.]+)=([^?|^&]+)");


        /// <summary>
        /// Converts the URI into a dictionary of settings
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> ParseUriSettings(string description)
        {
            var parameters = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(description))
            {
                return parameters;
            }

            var match = _regex.Match(description);
            while (match.Success)
            {
                var key = match.Groups[1].Value;
                if (parameters.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"An url parameter for a profile was specified twice: {key} in the uri \n {description}");
                }

                parameters.Add(key, match.Groups[2].Value);
                match = match.NextMatch();
            }

            return parameters;
        }

        public Profile GetOsmProfile(string profileName)
        {
            foreach (var p in OsmVehicleProfiles)
            {
                if (profileName.ToLower().Equals(p.Name.ToLower()))
                {
                    return p;
                }
            }

            return null;
        }
    }

    public static class HelperExtensions
    {
        public static T Value<T>(this Dictionary<string, string> dict, string key, T defaultValue = default(T))
        {
            if (!dict.ContainsKey(key)
                || string.IsNullOrEmpty(dict[key]))
            {
                return defaultValue;
            }

            if (typeof(T).FullName == "System.String")
            {
                // Hackety hack hack
                var v = (T) (object) dict[key];
                dict.Remove(key);
                return v;
            }

            var value = dict[key];
            dict.Remove(key);

            // Yep, this is cheating ;)
            return JToken.Parse(value).Value<T>();
        }

        public static string FixedId(this IOtherModeGenerator gen)
        {
            return gen.OtherModeIdentifier().Split("&")[0];
        }
    }

    class DummyOtherMode : IOtherModeGenerator
    {
        public uint TimeBetween(IStop from, IStop to)
        {
            throw new NotImplementedException();
        }

        public Dictionary<StopId, uint> TimesBetween(IStop from, IEnumerable<IStop> to)
        {
            throw new NotImplementedException();
        }

        public Dictionary<StopId, uint> TimesBetween(IEnumerable<IStop> @from, IStop to)
        {
            throw new NotImplementedException();
        }

        public uint Range()
        {
            return 0;
        }

        public string OtherModeIdentifier()
        {
            return "<some other mode identifier>";
        }

        public IOtherModeGenerator GetSource(StopId @from, StopId to)
        {
            throw new NotImplementedException();
        }
    }
}