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
            ExtractSegment<T>(this Databases dbs, Journey<T> j)
            where T : IJourneyMetric<T>
        {
            var connections = dbs.GetConnectionsReader();
            connections.MoveTo(j.Location.DatabaseId, j.Connection);
            var arrivalLocation =
                new TimedLocation(
                    dbs.LocationOf(j.Location),
                    j.Time,
                    connections.ArrivalDelay
                );

            var currTrip = j.TripId;
            var rest = j;
            while (rest.PreviousLink != null &&
                   currTrip.Equals(rest.TripId) &&
                   !rest.SpecialConnection)

            {
                rest = rest.PreviousLink;
            }

            connections.MoveTo(j.Location.DatabaseId, j.Connection);
            var departure = new TimedLocation(
                dbs.LocationOf(rest.Location),
                rest.Time,
                connections.DepartureDelay
            );


            var headSign = "";
            var route = "";

            // ReSharper disable once InvertIf
            var trips = dbs.GetTripsReader();
            if (trips.MoveTo(j.TripId))
            {
                trips.Attributes.TryGetValue("headsign", out headSign);
                trips.Attributes.TryGetValue("route", out route);
            }

            return (new Segment(departure, arrivalLocation, route, headSign), rest.PreviousLink);
        }

        public static Journey Translate<T>(
            this Databases dbs, Journey<T> j) where T : IJourneyMetric<T>
        {
            var segments = new List<Segment>();
            Segment segment;
            // Yep, we shorten j
            (segment, j) = dbs.ExtractSegment(j);
            segments.Add(segment);

            while (j != null)
            {
                (segment, j) = dbs.ExtractSegment(j);
                segments.Add(segment);
            }

            segments.Reverse();

            // We have reached the genesis, and should thus have all the segments
            return new Journey(segments);
        }

        public static List<Journey> Translate<T>(
            this Databases dbs, IEnumerable<Journey<T>> journeys)
            where T : IJourneyMetric<T>
        {
            var list = new List<Journey>();
            foreach (var j in journeys)
            {
                list.Add(dbs.Translate(j));
            }

            return list;
        }


        public static Location LocationOf(this Databases dbs, string globalId)
        {
            var stops = dbs.GetStopsReader();
            return !stops.MoveTo(globalId) ? null : new Location(stops);
        }

        private static Location LocationOf(this Databases dbs, LocationId localId)
        {
            var stops = dbs.GetStopsReader();
            return !stops.MoveTo(localId) ? null : new Location(stops);
        }

        internal static LocationSegmentsResult SegmentsForLocation
        (this Databases dbs,
            string globalId, DateTime time, TimeSpan window)
        {
            if (dbs == null) throw new ArgumentNullException(nameof(dbs));
            var stops = dbs.GetStopsReader();
            if (!stops.MoveTo(globalId))
            {
                return null;
            }

            var location = new Location(stops);
            var stop = stops.Id;

            var departureEnumerator = dbs.GetConnections();
            if (!departureEnumerator.MoveNext(time))
            {
                return new LocationSegmentsResult()
                {
                    Location = location,
                    Segments = new Segment[0]
                };
            }

            var trips = dbs.GetTripsReader();
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