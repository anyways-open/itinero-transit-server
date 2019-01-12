
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A segment represents a part of the journey, e.g. one link where you take the train    
    /// </summary>
    public class Segment
    {
        internal Segment(TimedLocation departure, TimedLocation arrival, string vehicle, string headSign)
        {
            Departure = departure;
            Arrival = arrival;
            Vehicle = vehicle;
            Headsign = headSign;
        }

        /// <summary>
        /// The departure location.
        /// </summary>
        public TimedLocation Departure { get; }
        
        /// <summary>
        /// The arrival location.
        /// </summary>
        public TimedLocation Arrival { get; }
        
        /// <summary>
        /// The identifier of the vehicle
        /// </summary>
        public string Vehicle { get; }
        
        /// <summary>
        /// The name of the train, e.g. its destination
        /// </summary>
        public string Headsign { get; }
    }
}