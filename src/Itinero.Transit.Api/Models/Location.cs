using System;
using System.Collections.Generic;
using Itinero.Transit.Data.Core;
using Serilog;

// ReSharper disable CollectionNeverQueried.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A location.
    /// </summary>
    public class Location
    {
        internal Location(Stop source)
            : this(source.GlobalId, null,
                (source.Longitude, source.Latitude))
        {
            try
            {
                var name = string.Empty;
                if (source.Attributes == null ||
                    !source.Attributes.TryGetValue("name", out name))
                {
                    Log.Verbose($"Name not found on stop");
                }

                Name = name;

                if (source.Attributes == null) return;
                foreach (var (key, value) in source.Attributes)
                {
                    if (key.StartsWith("name:"))
                    {
                        TranslatedNames[key.Substring(5)] = value;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Something went wrong getting the names of a stop {source.GlobalId}.");
            }
        }

        internal Location(string id, string name, (double lon, double lat) c)
        {
            Id = id;
            Name = name;
            Lat = c.lat;
            Lon = c.lon;
        }

        /// <summary>
        /// The latitude.
        /// </summary>
        public double Lat { get; }

        /// <summary>
        /// The longitude.
        /// </summary>
        public double Lon { get; }

        /// <summary>
        /// The id.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Eventual translations, will contain 'nl --> Brugge', 'fr --> Bruges', ... if the names are known
        /// </summary>
        public Dictionary<string, string> TranslatedNames { get; } = new Dictionary<string, string>();
    }
}