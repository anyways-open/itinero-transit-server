using System;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Journeys;

// ReSharper disable NotAccessedField.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api
{
    /// <summary>
    /// StopInfo is information about a certain stop in the journey.
    /// A stop is where the traveller gets on the train, gets off or transfers.
    /// </summary>
    internal class StopInfo<T> where T : IJourneyStats<T>
    {
        /// <summary>
        /// Info about the station. Yes, no camel case
        /// </summary>
        public readonly StationInfo Stationinfo;

        /// <summary>
        /// The station name. Redundant information, can also be found in 'stationinfo'
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

        public double Lat, Lon;


        public StopInfo(PublicTransportRouter router, Journey<T> connectionPart)
        {
            Stationinfo = router.GetLocationInfo(connectionPart.Location);
            Station = Stationinfo.Name;
            Vehicle = string.Empty;
            //DepartureConnection = string.Empty;
            Time = (long) connectionPart.Time;
            Delay = 0; // TODO add delay
            Lat = Stationinfo.LocationY;
            Lon = Stationinfo.LocationX;
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