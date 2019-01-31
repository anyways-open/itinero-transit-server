using System;
using Itinero.Transit.Data;
using Serilog;

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
                this.Name = name;
            }
            catch
            {
                Log.Information($"No name found for {source.GlobalId}");
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
    }
}