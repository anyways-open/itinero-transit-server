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
using Itinero.IO.Osm.Tiles;
using Serilog;


namespace Itinero.Transit.Api.Logic
{
    public class OtherModeBuilder
    {
        public readonly RouterDb RouterDb;

        public readonly
            Dictionary<string, Func<string, List<LocationId>, List<LocationId>, (IOtherModeGenerator, bool useCache)>>
            Factories =
                new Dictionary<string, Func<string, List<LocationId>, List<LocationId>, (IOtherModeGenerator, bool
                    useCache)>>();


        // First profile is the default profile
        public readonly List<Profile> OsmVehicleProfiles = new List<Profile>()
        {
            OsmProfiles.Pedestrian,
            OsmProfiles.Bicycle,
        };

        public OtherModeBuilder(
            string osmRoutableTilesCacheDirectory = null,
            IConfiguration configuration = null)
        {
            if (!string.IsNullOrEmpty(osmRoutableTilesCacheDirectory))
            {
                OsmTransferGenerator.EnableCaching(osmRoutableTilesCacheDirectory);
            }

            RouterDb = new RouterDb();
            RouterDb.DataProvider = new DataProvider(RouterDb);


            AddFactories();


            if (configuration == null) return;


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
                (str, __, _) =>
                {
                    var dict = ParseUriSettings(str);
                    var gen = new CrowsFlightTransferGenerator(
                        dict.Value("maxDistance", 500),
                        dict.Value("speed", 1.4f)
                    );
                    return (gen, true);
                });

            Factories.Add(
                // THis is only a dummy OsmTransferGenerator, used to get the identifying string
                // So we pass in a dummy routerdb which is not loaded
                new OsmTransferGenerator(RouterDb).FixedId(),
                (str, _, __) =>
                {
                    var dict = ParseUriSettings(str);
                    var profileName = dict.Value("profile", "pedestrian");
                    var profile = GetOsmProfile(profileName);
                    if (profile == null)
                    {
                        throw new KeyNotFoundException($"The profile {profileName} was not found");
                    }

                    var gen = new OsmTransferGenerator(RouterDb,
                        dict.Value("maxDistance", 500),
                        profile
                    );

                    return (gen, true);
                });

            Factories.Add(
                new InternalTransferGenerator().FixedId(),
                (str, _, __) =>
                {
                    var dict = ParseUriSettings(str);
                    var gen = new InternalTransferGenerator(dict.Value<uint>("timeNeeded", 180));
                    return (gen, false);
                });


            Factories.Add(
                new FirstLastMilePolicy(
                    new DummyOtherMode(), new DummyOtherMode(), new List<LocationId>(),
                    new DummyOtherMode(), new List<LocationId>()).FixedId(),
                (str, departures, arrivals) =>
                {
                    var dict = ParseUriSettings(str);
                    var defaultModeString =
                        new OsmTransferGenerator(RouterDb).OtherModeIdentifier();
                    var defaultMode = Uri.UnescapeDataString(dict.Value("default", defaultModeString));
                    var firstMile = Uri.UnescapeDataString(dict.Value("firstMile", defaultModeString));
                    var lastMile = Uri.UnescapeDataString(dict.Value("lastMile", defaultModeString));

                    if (dict.ContainsKey("profile") || dict.ContainsKey("maxDistance"))
                    {
                        throw new ArgumentException("First-Last-Mile contains improperly formatted subgenerators: " +
                                                    str);
                    }

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
                var mode = f.Value.Invoke("", new List<LocationId>(), new List<LocationId>());
                urls.Add(mode.Item1.OtherModeIdentifier());
            }

            return urls;
        }

        private readonly Dictionary<string, IOtherModeGenerator> _cachedOtherModeGenerators =
            new Dictionary<string, IOtherModeGenerator>();


        public IOtherModeGenerator Create(string description, List<LocationId> starts, List<LocationId> ends)
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

            var (walkGen, useCache) = Factories[fixedPart](description, starts, ends);

            if (walkGen.OtherModeIdentifier() != Uri.UnescapeDataString(description))
            {
                throw new Exception($"" +
                                    $"Something went very wrong here: the description does not match the generated value:\n" +
                                    $" expected:{description}\n" +
                                    $" Got: {walkGen.OtherModeIdentifier()}");
            }

            // ReSharper disable once InvertIf
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
        /// <param name="uri"></param>
        /// <returns></returns>
        private static IReadOnlyDictionary<string, string> ParseUriSettings(string description)
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
        public static T Value<T>(this IReadOnlyDictionary<string, string> dict, string key, T defaultValue = default(T))
        {
            if (!dict.ContainsKey(key)
                || string.IsNullOrEmpty(dict[key]))
            {
                return defaultValue;
            }

            if (typeof(T).FullName == "System.String")
            {
                // Hackety hack hack
                return (T) (object) dict[key];
            }

            // Yep, this is cheating ;)
            return JToken.Parse(dict[key]).Value<T>();
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

        public Dictionary<LocationId, uint> TimesBetween(IStop from, IEnumerable<IStop> to)
        {
            throw new NotImplementedException();
        }

        public float Range()
        {
            return 0.0f;
        }

        public string OtherModeIdentifier()
        {
            return "<some other mode identifier>";
        }
    }
}