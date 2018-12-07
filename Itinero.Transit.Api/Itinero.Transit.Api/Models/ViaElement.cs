// ReSharper disable MemberCanBePrivate.Global

using Itinero.IO.LC;
using Itinero.Transit.Api.Logic;

namespace Itinero.Transit.Api
{
    /// <summary>
    /// Represents a transfer
    /// </summary>
    public class ViaElement<T> where T : IJourneyStats<T>
    {
        /// <summary>
        /// Indictates how many transfers are before this transfer, in the journey
        /// </summary>
        public readonly int Id;

        public readonly StationInfo Stationinfo;
        public readonly int TimeBetween;

        public readonly StopInfo<T> Arrival, Departure;

        public ViaElement(PublicTransportRouter router, int id,
            Journey<T> previousConnection, Journey<T> transfer, Journey<T> nextConnection)
        {
            Id = id;
            Stationinfo = router.GetLocationInfo(previousConnection.Location);
            TimeBetween = (int) (transfer.Time - previousConnection.Time);
            Arrival = new StopInfo<T>(router, previousConnection);
            Departure = new StopInfo<T>(router, nextConnection);
        }

        public ViaElement(int id, StationInfo stationinfo, int timeBetween, StopInfo<T> arrival)
        {
            Id = id;
            Stationinfo = stationinfo;
            TimeBetween = timeBetween;
            Arrival = arrival;
        }
    }
}