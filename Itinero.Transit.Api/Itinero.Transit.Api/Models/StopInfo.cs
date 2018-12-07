using System;
using System.Linq;
using Itinero.Transit.Api.Logic;
// ReSharper disable NotAccessedField.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api
{
    /// <summary>
    /// StopInfo is information about a certain stop in the journey.
    /// A stop is where the traveller gets on the train, gets off or transfers.
    /// </summary>
    public class StopInfo<T> where T : IJourneyStats<T>
    {
        /// <summary>
        /// Info about the station. Yes, no camel case
        /// </summary>
        public readonly StationInfo Stationinfo;

        /// <summary>
        /// The station name. Redudant information, can also be found in 'stationinfo'
        /// </summary>
        public readonly string Station;


        /// <summary>
        /// Connection ID
        /// </summary>
        public readonly Uri DepartureConnection;


        /// <summary>
        /// Contains the headsign of the train that should be taken
        /// </summary>
        public readonly Direction Direction;

        /// <summary>
        /// The route-ID, in BE.NMBS.ROUTEID-format
        /// </summary>
        public readonly string Vehicle;


        /// <summary>
        /// Seconds since Unix Epoch
        /// </summary>
        public readonly long Time;


        /// <summary>
        /// Train delay in seconds
        /// </summary>
        public readonly int Delay;

        public float Lat, Lon;


        public StopInfo(PublicTransportRouter router, Journey<T> connectionPart)
        {
            var connection = router.GetConnection(connectionPart.Connection);
            
            Stationinfo = router.GetLocationInfo(connectionPart.Location);
            Station = Stationinfo.Name;
            var uri = router.GetConnectionUri(connectionPart.Connection);
            Vehicle = uri.Segments.Last();
            DepartureConnection = uri;
            Time = (long) connectionPart.Time;
            Delay = 0; // TODO add delay
            var loc = router.GetCoord(connectionPart.Location);
            Lat = loc.Lat;
            Lon = loc.Lon;
        }


        public StopInfo(StationInfo stationinfo, Uri departureConnection, string direction, string vehicle, int time,
            int delay, float lat, float lon)
        {
            Stationinfo = stationinfo;
            Station = stationinfo.Standardname;
            DepartureConnection = departureConnection;
            Vehicle = vehicle;
            Time = time;
            Delay = delay;
            Lat = lat;
            Lon = lon;
            Direction = new Direction(direction);
        }
    }


    public class Direction
    {
        public string Name;

        public Direction(string name)
        {
            Name = name;
        }
    }
}