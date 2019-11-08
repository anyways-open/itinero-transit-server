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
using Itinero.Transit.Logging;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

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

        private static (Segment, int newIndex) TranslateSegment<T>(this OperatorSet dbs, List<Journey<T>> parts, int i)
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
        private static Segment TranslateWalkSegment<T>(this OperatorSet dbs,
            Journey<T> j, CoordinatesCache coordinatesCache) where T : IJourneyMetric<T>
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

            var index = coordinatesCache.IndexFor<T>(fromStop, toStop);
            var (coordinates, license, description) = coordinatesCache.CommonCoordinates[index];

            if (coordinatesCache.Compress)
            {
                return new Segment(departureTimed, arrivalTimed,
                    description,
                    (uint) index,
                    (uint) (j.Time - j.PreviousLink.Time),
                    license);
            }

            return new Segment(
                departureTimed, arrivalTimed,
                description,
                coordinates,
                (uint) (j.Time - j.PreviousLink.Time),
                license
            );
        }


        /// <summary>
        /// Translates a single, forward journey into the Models which can be JSONified
        /// </summary>
        /// <returns></returns>
        public static Models.Journey Translate<T>(
            this OperatorSet dbs, Journey<T> journey, CoordinatesCache coordinatesCache) where T : IJourneyMetric<T>
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
                    var segment = dbs.TranslateWalkSegment(j, coordinatesCache);
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

        public static (List<Models.Journey>, List<List<Coordinate>>) Translate<T>
        (this OperatorSet dbs, IEnumerable<Journey<T>> journeys,
            IOtherModeGenerator walkGenerator, bool compress = false)
            where T : IJourneyMetric<T>
        {
            var list = new List<Models.Journey>();
            var commonCoordinates = new List<List<Coordinate>>();
            if (journeys == null)
            {
                return (list, commonCoordinates);
            }


            var coordinatesCache = new CoordinatesCache(walkGenerator, compress);
            foreach (var j in journeys)
            {
                list.Add(dbs.Translate(j, coordinatesCache));
            }

            if (compress)
            {
                commonCoordinates = coordinatesCache.CommonCoordinates.Select(c => c.Item1).ToList();
            }

            return (list, commonCoordinates);
        }


        public static Location LocationOf(this OperatorSet dbs, string globalId)
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
        (this OperatorSet dbs,
            string globalId, DateTime time, DateTime windowEnd)
        {
            if (dbs == null) throw new ArgumentNullException(nameof(dbs));

            if (dbs.EarliestLoadedTime() == DateTime.MaxValue || dbs.LatestLoadedTime() == DateTime.MaxValue)
            {
                var msg = $"No data is loaded in the database yet. Please come back later";
                throw new ArgumentException(msg);
            }

            if (!(dbs.EarliestLoadedTime() < time && windowEnd < dbs.LatestLoadedTime()))
            {
                var msg = $"The given time period is not loaded in the database: " +
                          $"You can only query between {dbs.EarliestLoadedTime():s} and {dbs.LatestLoadedTime():s}, \n" +
                          $"but you request connections between {time:s} and {windowEnd:s}";
                throw new ArgumentException(msg);
            }


            var stops = dbs.GetStopsReader();
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

        public static (List<Coordinate> coordinates, string generator, string license) GetCoordinatesFor(
            this IOtherModeGenerator walksGenerator, IStop fromStop,
            IStop toStop)
        {
            var coorFrom = new Coordinate(fromStop.Latitude, fromStop.Longitude);
            var coorTo = new Coordinate(toStop.Latitude, toStop.Longitude);

            var coordinates = new List<Coordinate>
            {
                coorFrom, coorTo
            };

            var gen = walksGenerator.GetSource(fromStop.Id, toStop.Id);

            var license = "";

            if (gen is OsmTransferGenerator osm)
            {
                license = "openstreetmap.org/copyright";
                var route = osm.CreateRoute((coorFrom.Lat, coorFrom.Lon), (coorTo.Lat, coorTo.Lon),
                    out var _, out var errorMessage);
                if (route == null)
                {
                    Log.Error(
                        $"Weird: got a journey with an OSM-route from {fromStop.GlobalId} to {toStop.GlobalId}, but now we can't calculate a route anymore... Error given is {errorMessage}");
                }
                else
                {
                    coordinates = route.Shape.Select(
                        coor => new Coordinate(coor.Latitude, coor.Longitude)).ToList();
                }
            }

            return (coordinates, gen.OtherModeIdentifier(), license);
        }
    }

    public struct CoordinatesCache
    {
        public readonly bool Compress;
        private readonly IOtherModeGenerator _walksGenerator;
        public readonly List<(List<Coordinate>, string license, string generator)> CommonCoordinates;
        private readonly Dictionary<string, int> _index;

        public CoordinatesCache(IOtherModeGenerator walksGenerator, bool compress)
        {
            _walksGenerator = walksGenerator;
            Compress = compress;

            _index = new Dictionary<string, int>();
            CommonCoordinates = new List<(List<Coordinate>, string license, string generator)>();
        }

        public int IndexFor<T>(IStop fromStop, IStop toStop)
            where T : IJourneyMetric<T>
        {
            var coorFrom = new Coordinate(fromStop.Latitude, fromStop.Longitude);
            var coorTo = new Coordinate(toStop.Latitude, toStop.Longitude);

            var key = $"{coorFrom.Lat},{coorFrom.Lon},{coorTo.Lat},{coorTo.Lon}";
            if (_index.TryGetValue(key, out var index))
            {
                // Already done!
                return index;
            }


            index = CommonCoordinates.Count;
            var (coordinates, generator, license) = _walksGenerator.GetCoordinatesFor(fromStop, toStop);

            CommonCoordinates.Add((coordinates, generator, license));
            _index.Add(key, index);

            return index;
        }
    }
}