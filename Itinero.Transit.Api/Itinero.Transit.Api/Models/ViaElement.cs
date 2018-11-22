// ReSharper disable MemberCanBePrivate.Global

using Itinero.Transit.Api.Logic;

namespace Itinero.Transit.Api
{
    /// <summary>
    /// Represents a transfer
    /// </summary>
    public class ViaElement
    {
        /// <summary>
        /// Indictates how many transfers are before this transfer, in the journey
        /// </summary>
        public readonly int Id;

        public readonly StationInfo Stationinfo;
        public readonly int TimeBetween;

        public readonly StopInfo Arrival, Departure;

        public ViaElement(PublicTransportRouter router, int id, IConnection previousConnection, IConnection nextConnection)
        {
            Id = id;
            Stationinfo = router.GetLocationInfo(previousConnection.ArrivalLocation());
            TimeBetween = (int) (nextConnection.DepartureTime() - previousConnection.ArrivalTime()).TotalSeconds;
            
            Arrival = new StopInfo(router, previousConnection, departure : false);
            Departure = new StopInfo(router, nextConnection, departure: true);
        }

        public ViaElement(int id, StationInfo stationinfo, int timeBetween, StopInfo arrival)
        {
            Id = id;
            Stationinfo = stationinfo;
            TimeBetween = timeBetween;
            Arrival = arrival;
        }
    }
}