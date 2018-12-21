
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

        public Segment(TimedLocation departure, TimedLocation arrival)
        {
            Departure = departure;
            Arrival = arrival;
        }
    }
}