using Itinero.Transit.Data;
using Serilog;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api.Models
{
    public class Location
    {

        public double Lat, Lon;
        public string Id, Name;

        public Location(IStop source)
            : this(source.GlobalId, null,
                source.Latitude, source.Longitude)
        {
            try
            {
                source.Attributes.TryGetValue("name", out Name);
            }
            catch
            {
                Log.Information($"No name found for {source.GlobalId}");
            }
        }
        
        public Location(string id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
        }
    }
}