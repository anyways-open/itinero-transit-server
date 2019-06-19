using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Takes Itinero.Transit-objects and translates them into our models
    /// </summary>
    public static class JourneyTranslator
    {
        private static (Segment segment, Journey<T> rest)
            ExtractSegment<T>(this State dbs, Journey<T> j)
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


            var headSign = "";
            var route = "";

            // ReSharper disable once InvertIf
            var trips = dbs.GetTripsReader();
            if (trips.MoveTo(j.TripId))
            {
                trips.Attributes.TryGetValue("headsign", out headSign);
                trips.Attributes.TryGetValue("route", out route);
            }

            route = route ?? "";
            headSign = headSign ?? "";


            var rest = j;
            while (rest.PreviousLink != null &&
                   currTrip.Equals(rest.TripId) &&
                   !rest.SpecialConnection)

            {
                rest = rest.PreviousLink;
            }


            connections.MoveTo(rest.Location.DatabaseId, rest.Connection);
            var departure = new TimedLocation(
                dbs.LocationOf(rest.Location),
                rest.Time,
                connections.DepartureDelay
            );


            return (new Segment(departure, arrivalLocation, route, headSign), rest);
        }

        private static Models.Journey Translate<T>(
            this State dbs, Journey<T> j) where T : IJourneyMetric<T>
        {
            var segments = new List<Segment>();
            var takenVehicleCount = 0;
            while (j != null)
            {
                if (j.SpecialConnection)
                {
                    switch (j.Connection)
                    {
                        case Journey<T>.GENESIS:
                        case Journey<T>.OTHERMODE:
                        {
                            var arr = new TimedLocation(
                                dbs.LocationOf(j.Location), j.Time, 0);
                            j = j.PreviousLink;
                            var dep = new TimedLocation(
                                dbs.LocationOf(j.Location), j.Time, 0);
                            segments.Add(new Segment(dep, arr, "", "Walk/Transfer to"));
                            continue;
                        }
                        default: break;
                    }
                }

                Segment segment;
                var oldJ = j;
                (segment, j) = dbs.ExtractSegment(j);
                segments.Add(segment);
                takenVehicleCount++;
                if (Equals(j, oldJ))
                {
                    j = j.PreviousLink;
                }
            }

            segments.Reverse();

            // We have reached the genesis, and should thus have all the segments
            return new Models.Journey(segments, takenVehicleCount-1);
        }

        public static List<Models.Journey> Translate<T>(
            this State dbs, IEnumerable<Journey<T>> journeys)
            where T : IJourneyMetric<T>
        {
            var list = new List<Models.Journey>();
            foreach (var j in journeys)
            {
                list.Add(dbs.Translate(j));
            }

            return list;
        }


        public static Location LocationOf(this State dbs, string globalId)
        {
            var stops = dbs.GetStopsReader();
            return !stops.MoveTo(globalId) ? null : new Location(stops);
        }

        private static Location LocationOf(this State dbs, LocationId localId)
        {
            var stops = dbs.GetStopsReader();
            return !stops.MoveTo(localId) ? null : new Location(stops);
        }

        internal static LocationSegmentsResult SegmentsForLocation
        (this State dbs,
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