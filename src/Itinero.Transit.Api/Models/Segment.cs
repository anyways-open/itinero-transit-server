
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A segment represents a part of the journey, e.g. one link where you take the train    
    /// </summary>
    public class Segment
    {

        public TimedLocation Departure, Arrival;
        
        /// <summary>
        /// The identifier of the vehicle
        /// </summary>
        public string Vehicle;
        /// <summary>
        /// The name of the train, e.g. its destination
        /// </summary>
        public string HeadSign;

        public Segment(TimedLocation departure, TimedLocation arrival, string vehicle, string headSign)
        {
            Departure = departure;
            Arrival = arrival;
            Vehicle = vehicle;
            HeadSign = headSign;
        }
    }
}