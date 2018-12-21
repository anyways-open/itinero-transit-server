using System;
using System.Collections.Generic;

// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    public class Journey
    {
        /// <summary>
        ///  All the individual segments:
        /// one segment for each train/bus/... the traveller takes
        /// </summary>
        public readonly List<Segment> Segments;

        /// <summary>
        /// The departure time and location of this journey. Same as `segments.first().departure`
        /// </summary>
        public TimedLocation Departure;

        /// <summary>
        /// The arrival time and location of this journey. Same as `segments.last().arrival`
        /// </summary>
        public TimedLocation Arrival;

        /// <summary>
        /// The total travel time in seconds. Equals `arrival.time - departure.time
        /// </summary>
        public int TravelTime;
        /// <summary>
        /// The total number of intermediate transfers. Equals `segments.Length - 1`
        /// </summary>
        public int Transfers;


        public Journey(List<Segment> segments)
        {
            Segments = segments;

            Departure = segments[0].Departure;

            var last = segments[segments.Count - 1];
            Arrival = last.Arrival;

            TravelTime = (int) (last.Arrival.Time - segments[0].Departure.Time).TotalSeconds;
            Transfers = segments.Count - 1;
        }
    }
}