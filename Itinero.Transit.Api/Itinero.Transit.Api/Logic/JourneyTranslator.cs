using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Takes Itinero.Transit-objects and translates them into our models
    /// </summary>
    public class JourneyTranslator
    {
        public readonly DatabaseLoader Db;
        private readonly StopsDb.StopsDbReader _stops;

        public JourneyTranslator(DatabaseLoader db)
        {
            Db = db;
            _stops = Db.Stops.GetReader();
        }


        private (Segment segment, Journey<T> rest) ExtractSegment<T>(Journey<T> j)
            where T : IJourneyStats<T>
        {
            var arrivalLocation =
                new TimedLocation(
                    LocationOf(j.Location),
                    j.Time);

            var currTrip = j.TripId;
            var rest = j;
            do
            {
                rest = rest.PreviousLink;
            } while (rest.PreviousLink != null && currTrip == rest.TripId && !rest.SpecialConnection);

            var departure = new TimedLocation(
                LocationOf(rest.Location),
                rest.Time);
            return (new Segment(departure, arrivalLocation), rest.PreviousLink);
        }

        public Journey Translate<T>(Journey<T> j) where T : IJourneyStats<T>
        {
            var segments = new List<Segment>();
            Segment segment;
            // Yep, we shorten j
            (segment, j) = ExtractSegment(j);
            segments.Add(segment);

            while (j != null)
            {
                (segment, j) = ExtractSegment(j);
                segments.Add(segment);
            }
            
            segments.Reverse();

            // We have reached the genesis, and should thus have all the segments
            return new Journey(segments);
        }

        public List<Journey> Translate<T>(IEnumerable<Journey<T>> journeys)
            where T : IJourneyStats<T>
        {
            var list = new List<Journey>();
            foreach (var j in journeys)
            {
                list.Add(Translate(j));
            }

            return list;
        }


        public Location LocationOf(string globalId)
        {
            return !_stops.MoveTo(globalId) ? null : new Location(_stops);
        }

        public Location LocationOf((uint, uint) localId)
        {
            return !_stops.MoveTo(localId) ? null : new Location(_stops);
        }

        public (uint, uint) InternalidOf(string globalId)
        {
            if (!_stops.MoveTo(globalId))
            {
                throw new Exception($"Id {globalId} not found");
            }

            return _stops.Id;
        }
    }
}