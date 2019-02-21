using System.Collections.Generic;
using Itinero.Transit.Data;
using Serilog;

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
                source.Attributes.TryGetValue("name", out var name);
                Name = name;
            }
            catch
            {
                Log.Information($"No name found for {source.GlobalId}");
            }


            foreach (var attr in source.Attributes)
            {
                if (attr.Key.StartsWith("name:"))
                {
                    TranslatedNames[attr.Key.Substring(5)] = attr.Value;
                }
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