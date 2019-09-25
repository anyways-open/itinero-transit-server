using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic.Transfers;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Serilog;

// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Takes Itinero.Transit-objects and translates them into our models
    /// </summary>
    public static class JourneyTranslator
    {
        private static readonly List<string> _colours = new List<string>
        {
            "#002e63",
            "#2e3f57",
            "#5c514b",
            "#8a623f",
            "#b87333"
        };


        public static Geojson AsGeoJson(this Models.Journey j)
        {
            var features = new List<Feature>();
            uint i = 0;
            foreach (var segment in j.Segments)
            {
                i++;
                var coors = segment.Coordinates ??
                            new List<Coordinate>
                            {
                                new Coordinate(segment.Departure.Location.Lat, segment.Departure.Location.Lon),
                                new Coordinate(segment.Arrival.Location.Lat, segment.Arrival.Location.Lon)
                            };
                var geo = new Geometry(coors);
                var f = new Feature(geo, new Properties(_colours[(int) (i % _colours.Count)]));
                features.Add(f);
            }

            return new Geojson(features);
        }

        private static (Segment, int newIndex) TranslateSegment<T>(this State dbs, List<Journey<T>> parts, int i)
            where T : IJourneyMetric<T>
        {
            var stops = dbs.GetStopsReader().AddOsmReader();

            var j = parts[i];
            var connectionReader = dbs.GetConnectionsReader();
            var connection = connectionReader.Get(j.Connection);


            // Get all the trip info for this segment
            var tripReader = dbs.GetTripsReader();
            var trip = new Trip();
            tripReader.Get(j.TripId, trip);
            var vehicleId = trip.GlobalId ?? "";
            if (!trip.Attributes.TryGetValue("headsign", out var headsign))
            {
                headsign = "";
            }


            // Get the departure information
            var departure = stops.LocationOf(connection.DepartureStop);
            var departureTimed = new TimedLocation(
                departure,
                connection.DepartureTime.FromUnixTime(),
                connection.DepartureDelay);


            // Now, we walk along the journey to find the end of this segment
            // In the meanwhile, record intermediate stops
            var allStations = new List<TimedLocation> {departureTimed};

            // This loops walks further down the journey
            // It will let 'i' point to the latest element of this segment
            while (true)
            {
                i++;
                if (i >= parts.Count)
                {
                    // Oops, reached the last element
                    i--;
                    break;
                }

                var p = parts[i];
                if (p.SpecialConnection)
                {
                    // We are too far!
                    // This part is a transfer/walk/...
                    i--;
                    break;
                }

                // ReSharper disable once InvertIf
                if (!p.TripId.Equals(j.TripId))
                {
                    // We are too far!
                    // This part has a different trip id
                    i--;
                    break;
                }

                // We pass a stop
                // We should add this to the intermediate stops
                var loc = stops.LocationOf(parts[i].Location);

                var tloc = new TimedLocation(loc, parts[i].Time, connection.ArrivalDelay);
                allStations.Add(tloc);
            }
            // At this point, parts[i] is the last part of our journey

            j = parts[i];

            connectionReader.Get(j.Connection, connection);
            var arrival = stops.LocationOf(connection.ArrivalStop);
            var arrivalTimed = new TimedLocation(arrival,
                connection.ArrivalTime, connection.ArrivalDelay);

            if (!allStations.Last().Location.Id.Equals(arrival.Id))
            {
                allStations.Add(arrivalTimed);
            }

            return (new Segment(departureTimed, arrivalTimed, vehicleId, headsign, allStations), i);
        }

        /// <summary>
        /// Extract a single segment which has a special status
        /// Can be null if unneeded
        /// </summary>
        /// <returns></returns>
        public static Segment TranslateWalkSegment<T>(this State dbs,
            Journey<T> j, IOtherModeGenerator walksgenerator) where T : IJourneyMetric<T>
        {
            var stops = StopsReaderAggregator.CreateFrom(dbs.All()).AddOsmReader();
            if (j.Location.Equals(j.PreviousLink.Location))
            {
                // Object represent a transfer without moving...
                // We skip this if the next is an othermode as well
                return null;
            }

            // This is a piece where we walk/cycle/... or some other continuous transportation mode
            // Lets try to figure out what exactly we are doing

            var departure = stops.LocationOf(j.PreviousLink.Location);
            var departureTimed = new TimedLocation(
                departure, j.PreviousLink.Time, 0);
            var arrival = stops.LocationOf(j.Location);
            var arrivalTimed = new TimedLocation(
                arrival, j.Time, 0);

            stops.MoveTo(j.PreviousLink.Location);
            var fromStop = new Stop(stops);
            stops.MoveTo(j.Location);
            var toStop = new Stop(stops);

            var (coordinates, descriptor) =
                walksgenerator.GetCoordinatesFor(fromStop, toStop);

            return new Segment(
                departureTimed, arrivalTimed,
                descriptor,
                coordinates,
                (uint) (j.Time - j.PreviousLink.Time)
            );
        }

        public static (List<Coordinate>, string identifier) GetCoordinatesFor(this IOtherModeGenerator walksgenerator,
            Stop from, Stop to)
        {
            var walkGenerator = walksgenerator?.GetSource(from.Id, to.Id);

            List<Coordinate> coordinates = null;
            if (walkGenerator is OsmTransferGenerator osm)
            {
                var route = osm.CreateRoute(
                    ((float) from.Latitude, (float) from.Longitude),
                    ((float) to.Latitude, (float) to.Longitude), out _, out var errorMessage);
                if (route == null)
                {
                    Log.Error(
                        $"Weird: got a journey with an OSM-route from {from.GlobalId} to {to.GlobalId}, but now we can't calculate a route anymore... Error given is {errorMessage}");
                }
                else
                {
                    coordinates = route.Shape.Select(
                        coor => new Coordinate(coor.Latitude, coor.Longitude)).ToList();
                }
            }

            if (coordinates == null)
            {
                coordinates = new List<Coordinate>
                {
                    new Coordinate(from.Latitude, from.Longitude),
                    new Coordinate(to.Latitude, to.Longitude)
                };
            }


            return (coordinates, walksgenerator?.OtherModeIdentifier() ?? "?");
        }

        /// <summary>
        /// Translates a single, forward journey into the Models which can be JSONified
        /// </summary>
        /// <returns></returns>
        public static Models.Journey Translate<T>(
            this State dbs, Journey<T> journey, IOtherModeGenerator walkGenerator) where T : IJourneyMetric<T>
        {
            var parts = journey.ToList(); //Creates a list of the journey


            var segments = new List<Segment>();
            var vehiclesTaken = 0;


            // Skip the first connection, that is the boring genesis anyway
            for (var i = 1; i < parts.Count; i++)
            {
                var j = parts[i];

                if (!j.SpecialConnection)
                {
                    Segment segment;
                    (segment, i) = dbs.TranslateSegment(parts, i);
                    segments.Add(segment);
                    vehiclesTaken++;
                }
                else if (j.Connection.Equals(Journey<T>.OTHERMODE))
                {
                    var segment = dbs.TranslateWalkSegment(j, walkGenerator);
                    if (segment != null)
                    {
                        segments.Add(segment);
                    }
                }
                else
                {
                    throw new Exception("Case fallthrough: j is special but has an unrecognized special mode");
                }
            }


            return new Models.Journey(segments, vehiclesTaken);
        }

        public static List<Models.Journey> Translate<T>(this State dbs, IEnumerable<Journey<T>> journeys,
            IOtherModeGenerator walkGenerator)
            where T : IJourneyMetric<T>
        {
            var list = new List<Models.Journey>();
            if (journeys == null)
            {
                return list;
            }

            foreach (var j in journeys)
            {
                list.Add(dbs.Translate(j, walkGenerator));
            }

            return list;
        }


        public static Location LocationOf(this State dbs, string globalId)
        {
            var stops = dbs.GetStopsReader().AddOsmReader();
            return !stops.MoveTo(globalId) ? null : new Location(stops);
        }

        private static Location LocationOf(this IStopsReader stops, StopId localId)
        {
            if (!stops.MoveTo(localId))
            {
                throw new NullReferenceException("Location " + localId + " not found");
            }

            return new Location(stops);
        }


        /// <summary>
        /// Gives all the connections departing at the given location within the timewindow
        /// </summary>
        internal static LocationSegmentsResult SegmentsForLocation
        (this State dbs,
            string globalId, DateTime time, DateTime windowEnd)
        {
            if (dbs == null) throw new ArgumentNullException(nameof(dbs));
            var stops = dbs.GetStopsReader().AddOsmReader();
            if (!stops.MoveTo(globalId))
            {
                return null;
            }

            var location = new Location(stops);
            var stop = stops.Id;

            var departureEnumerator = dbs.GetConnections();
            departureEnumerator.MoveTo(time.ToUnixTime());


            if (!departureEnumerator.MoveNext())
            {
                return new LocationSegmentsResult
                {
                    Location = location,
                    Segments = new Segment[0]
                };
            }

            var trips = dbs.GetTripsReader();
            var timeMax = windowEnd.ToUnixTime();
            var segments = new List<Segment>();
            do
            {
                var connection = new Connection();
                departureEnumerator.Current(connection);

                if (!connection.DepartureStop.Equals(stop)) continue;
                if (connection.DepartureTime >= timeMax) break;

                var trip = new Trip();
                var headSign = "";
                var route = "";
                if (trips.Get(connection.TripId, trip))
                {
                    trip.Attributes.TryGetValue("headsign", out headSign);
                    trip.Attributes.TryGetValue("route", out route);
                }


                if (!stops.MoveTo(connection.DepartureStop)) continue;
                var departure = new TimedLocation(new Location(stops),
                    connection.DepartureTime, connection.DepartureDelay);

                if (!stops.MoveTo(connection.ArrivalStop)) continue;
                var arrival = new TimedLocation(new Location(stops),
                    connection.ArrivalTime, connection.ArrivalDelay);


                segments.Add(new Segment(departure, arrival, route, headSign, null));
            } while (departureEnumerator.MoveNext());


            return new LocationSegmentsResult
            {
                Location = location,
                Segments = segments.ToArray()
            };
        }
    }
}