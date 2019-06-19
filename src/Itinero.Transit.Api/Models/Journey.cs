using System.Collections.Generic;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

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
        internal Journey(List<Segment> segments, int vehiclesTaken)
        {
            Segments = segments;

            Departure = segments[0].Departure;

            var last = segments[segments.Count - 1];
            Arrival = last.Arrival;

            TravelTime = (int) (last.Arrival.Time - segments[0].Departure.Time).TotalSeconds;
            VehiclesTaken = vehiclesTaken;
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
        /// The total number PT-vehicles taken. Often Segments.Count - 1
        /// </summary>
        public int VehiclesTaken { get; }
    }
}