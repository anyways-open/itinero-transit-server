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
using Serilog;


namespace Itinero.Transit.Api.Logic
{
    public class OtherModeBuilder
    {
        public readonly Dictionary<string, Func<string, List<LocationId>, List<LocationId>, IOtherModeGenerator>>
            Factories =
                new Dictionary<string, Func<string, List<LocationId>, List<LocationId>, IOtherModeGenerator>>();


        // First profile is the default profile
        public readonly List<Profile> OsmVehicleProfiles = new List<Profile>()
        {
            OsmProfiles.Pedestrian,
            OsmProfiles.Bicycle,
        };

        public OtherModeBuilder(IConfiguration configuration = null)
        {
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
                    var dict = Parse(str);
                    return new CrowsFlightTransferGenerator(
                        dict.Value("maxDistance", 500),
                        dict.Value("speed", 1.4f)
                    );
                });

            Factories.Add(
                new OsmTransferGenerator().FixedId(),
                (str, _, __) =>
                {
                    var dict = Parse(str);
                    var profileName = dict.Value("profile", "pedestrian");
                    var profile = OsmVehicleProfiles[0];
                    foreach (var p in OsmVehicleProfiles)
                    {
                        if (p.Name == profileName)
                        {
                            profile = p;
                        }
                    }

                    return new OsmTransferGenerator(
                        dict.Value("maxDistance", 500),
                        profile
                    );
                });

            Factories.Add(
                new InternalTransferGenerator().FixedId(),
                (str, _, __) =>
                {
                    var dict = Parse(str);
                    return new InternalTransferGenerator(dict.Value<uint>("timeNeeded", 180));
                });


            Factories.Add(
                new FirstLastMilePolicy(
                    new DummyOtherMode(), new DummyOtherMode(), new List<LocationId>(),
                    new DummyOtherMode(), new List<LocationId>()).FixedId(),
                (str, departures, arrivals) =>
                {
                    var dict = Parse(str);
                    var defaultModeString = new OsmTransferGenerator().OtherModeIdentifier();
                    var defaultMode = dict.Value("default", defaultModeString);
                    var firstMile = dict.Value("firstMile", defaultModeString);
                    var lastMile = dict.Value("lastMile", defaultModeString);


                    return new FirstLastMilePolicy(
                        Create(defaultMode, departures, arrivals),
                        Create(firstMile, departures, arrivals),
                        departures,
                        Create(lastMile, departures, arrivals),
                        arrivals
                    );
                }
            );
        }

        public List<string> SupportedUrls()
        {
            var urls = new List<string>();

            foreach (var f in Factories)
            {
                var mode = f.Value.Invoke("", new List<LocationId>(), new List<LocationId>());
                urls.Add(mode.OtherModeIdentifier());
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

            var walkGen = Factories[fixedPart](description, starts, ends).UseCache();
            _cachedOtherModeGenerators[description] = walkGen;
            return walkGen;
        }


        private static readonly Regex _regex = new Regex(@"[?|&]([\w\.]+)=([^?|^&]+)");


        public static IReadOnlyDictionary<string, string> Parse(string uri)
        {
            var parameters = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(uri))
            {
                return parameters;
            }

            var match = _regex.Match(new Uri(uri).PathAndQuery);
            while (match.Success)
            {
                parameters.Add(match.Groups[1].Value, match.Groups[2].Value);
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