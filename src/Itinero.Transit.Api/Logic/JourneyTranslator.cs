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
    public static class JourneyTranslator
    {
        private static (Segment segment, Journey<T> rest)
            ExtractSegment<T>(this TransitDb.TransitDbSnapShot snapShot, Journey<T> j)
            where T : IJourneyMetric<T>
        {
            var connections = snapShot.ConnectionsDb.GetReader();
            connections.MoveTo(j.Connection);
            var arrivalLocation =
                new TimedLocation(
                    snapShot.LocationOf(j.Location),
                    j.Time,
                    connections.ArrivalDelay
                );

            var currTrip = j.TripId;
            var rest = j;
            do
            {
                rest = rest.PreviousLink;
            } while (rest.PreviousLink != null &&
                     currTrip.Equals(rest.TripId) &&
                     !rest.SpecialConnection);

            connections.MoveTo(j.Connection);
            var departure = new TimedLocation(
                snapShot.LocationOf(rest.Location),
                rest.Time,
                connections.DepartureDelay
            );


            var headSign = "";
            var route = "";

            // ReSharper disable once InvertIf
            var trips = snapShot.TripsDb.GetReader();
            if (trips.MoveTo(j.TripId))
            {
                trips.Attributes.TryGetValue("headsign", out headSign);
                trips.Attributes.TryGetValue("route", out route);
            }

            return (new Segment(departure, arrivalLocation, route, headSign), rest.PreviousLink);
        }

        public static Journey Translate<T>(
            this TransitDb.TransitDbSnapShot snapShot, Journey<T> j) where T : IJourneyMetric<T>
        {
            var segments = new List<Segment>();
            Segment segment;
            // Yep, we shorten j
            (segment, j) = snapShot.ExtractSegment(j);
            segments.Add(segment);

            while (j != null)
            {
                (segment, j) = snapShot.ExtractSegment(j);
                segments.Add(segment);
            }

            segments.Reverse();

            // We have reached the genesis, and should thus have all the segments
            return new Journey(segments);
        }

        public static List<Journey> Translate<T>(
            this TransitDb.TransitDbSnapShot tdb, IEnumerable<Journey<T>> journeys)
            where T : IJourneyMetric<T>
        {
            var list = new List<Journey>();
            foreach (var j in journeys)
            {
                list.Add(tdb.Translate(j));
            }

            return list;
        }


        public static Location LocationOf(this TransitDb.TransitDbSnapShot tdb, string globalId)
        {
            var stops = tdb.StopsDb.GetReader();
            return !stops.MoveTo(globalId) ? null : new Location(stops);
        }

        private static Location LocationOf(this TransitDb.TransitDbSnapShot tdb, LocationId localId)
        {
            var stops = tdb.StopsDb.GetReader();
            return !stops.MoveTo(localId) ? null : new Location(stops);
        }

        internal static LocationSegmentsResult SegmentsForLocation(this TransitDb.TransitDbSnapShot latest,
            string globalId, DateTime time, TimeSpan window)
        {
            var stops = latest.StopsDb.GetReader();
            if (!stops.MoveTo(globalId))
            {
                return null;
            }

            var location = new Location(stops);
            var stop = stops.Id;

            var departureEnumerator = latest.ConnectionsDb.GetDepartureEnumerator();
            if (!departureEnumerator.MoveNext(time))
            {
                return new LocationSegmentsResult()
                {
                    Location = location,
                    Segments = new Segment[0]
                };
            }

            var trips = latest.TripsDb.GetReader();
            var timeMax = time.Add(window).ToUnixTime();
            var segments = new List<Segment>();
            while (departureEnumerator.MoveNext())
            {
                var connection = (IConnection) departureEnumerator;
                if (!departureEnumerator.DepartureStop.Equals(stop)) continue;
                if (departureEnumerator.DepartureTime >= timeMax) break;

                if (!trips.MoveTo(departureEnumerator.TripId)) continue;
                trips.Attributes.TryGetValue("headsign", out var headSign);
                trips.Attributes.TryGetValue("route", out var route);

                if (!stops.MoveTo(departureEnumerator.DepartureStop)) continue;
                var departure = new TimedLocation(new Location(stops),
                    connection.DepartureTime, connection.DepartureDelay);

                if (!stops.MoveTo(departureEnumerator.ArrivalStop)) continue;
                var arrival = new TimedLocation(new Location(stops),
                    connection.ArrivalTime, connection.ArrivalDelay);


                segments.Add(new Segment(departure, arrival, route, headSign));
            }

            return new LocationSegmentsResult()
            {
                Location = location,
                Segments = segments.ToArray()
            };
        }
    }
}