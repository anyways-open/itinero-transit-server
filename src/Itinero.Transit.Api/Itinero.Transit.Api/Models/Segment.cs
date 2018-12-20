
// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
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