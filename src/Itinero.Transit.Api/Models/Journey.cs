using System.Collections.Generic;

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A journey.
    /// </summary>
    public class Journey
    {
        /// <summary>
        /// Creates a new journey.
        /// </summary>
        /// <param name="segments"></param>
        internal Journey(List<Segment> segments)
        {
            Segments = segments;

            Departure = segments[0].Departure;

            var last = segments[segments.Count - 1];
            Arrival = last.Arrival;

            TravelTime = (int) (last.Arrival.Time - segments[0].Departure.Time).TotalSeconds;
            Transfers = segments.Count - 1;
        }
        
        /// <summary>
        ///  All the individual segments:
        /// one segment for each train/bus/... the traveller takes
        /// </summary>
        public List<Segment> Segments { get; }

        /// <summary>
        /// The departure time and location of this journey. Same as `segments.first().departure`
        /// </summary>
        public TimedLocation Departure { get; }

        /// <summary>
        /// The arrival time and location of this journey. Same as `segments.last().arrival`
        /// </summary>
        public TimedLocation Arrival { get; }

        /// <summary>
        /// The total travel time in seconds. Equals `arrival.time - departure.time
        /// </summary>
        public int TravelTime { get; }
        
        /// <summary>
        /// The total number of intermediate transfers. Equals `segments.Length - 1`
        /// </summary>
        public int Transfers { get; }
    }
}