using System;
using System.Linq;
using Itinero.Transit.Api.Logic;

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api
{
    /// <summary>
    /// StopInfo is information about a certain stop in the journey.
    /// A stop is where the traveller gets on the train, gets off or transfers.
    /// </summary>
    public class StopInfo
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


        public StopInfo(PublicTransportRouter router, IConnection connection, bool departure)
        {
            Stationinfo = router.GetLocationInfo(
                departure
                    ? connection.DepartureLocation()
                    : connection.ArrivalLocation());
            Station = Stationinfo.Name;
            Vehicle = connection.Route().Segments.Last();
            DepartureConnection = connection.Id();
            Time = (int) Math.Floor(((departure ? connection.DepartureTime() : connection.ArrivalTime())
                                     - new DateTime(1970, 1, 1)).TotalSeconds);
            Delay = 0; // TODO add delay
        }


        public StopInfo(StationInfo stationinfo, Uri departureConnection, string direction, string vehicle, int time,
            int delay)
        {
            Stationinfo = stationinfo;
            Station = stationinfo.Standardname;
            DepartureConnection = departureConnection;
            Vehicle = vehicle;
            Time = time;
            Delay = delay;
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