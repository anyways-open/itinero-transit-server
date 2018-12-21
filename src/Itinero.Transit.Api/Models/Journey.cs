using System;
using System.Collections.Generic;

// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    public class Journey
    {
    
        public readonly List<Segment> Segments;

        public TimedLocation Departure, Arrival;
        public int TravelTime, Transfers;


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