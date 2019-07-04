namespace Itinero.Transit.Api.Models
{
    public class Coordinate
    {
        public readonly double Lat, Lon;

        public Coordinate(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }
    }
}