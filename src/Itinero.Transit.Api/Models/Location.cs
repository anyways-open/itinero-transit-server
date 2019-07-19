using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Serilog;
// ReSharper disable CollectionNeverQueried.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A location.
    /// </summary>
    public class Location
    {
        internal Location(IStop source)
            : this(source.GlobalId, null,
                source.Latitude, source.Longitude)
        {
            try
            {
                var name = string.Empty;
                if (source.Attributes == null ||
                    !source.Attributes.TryGetValue("name", out name))
                {
                    Log.Verbose($"Name not found on stop {source.Id}");
                }

                Name = name;

                if (source.Attributes == null) return;
                foreach (var attr in source.Attributes)
                {
                    if (attr.Key.StartsWith("name:"))
                    {
                        TranslatedNames[attr.Key.Substring(5)] = attr.Value;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"Something went wrong getting the names of a stop {source.GlobalId}.");
            }
        }

        internal Location(string id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
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