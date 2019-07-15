using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.Journey;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Takes Itinero.Transit-objects and translates them into our models
    /// </summary>
    public static class JourneyTranslator
    {
        private static List<string> _colours = new List<string>
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

        /// <summary>
        /// Translates a single, forward journey into the Models which can be JSONified
        /// </summary>
        /// <returns></returns>
        public static Models.Journey Translate<T>(
            this State dbs, Journey<T> journey, IOtherModeGenerator walkGenerator) where T : IJourneyMetric<T>
        {
            var parts = journey.ToList(); // Puts genesis neatly at the start
            var segments = new List<Segment>();

            var vehiclesTaken = 0;


            var connection = dbs.GetConnectionsReader();
            var trip = dbs.GetTripsReader();

            // Skip the first connection, that is the boring genesis anyway
            for (var i = 1; i < parts.Count; i++)
            {
                var j = parts[i];

                if (!j.SpecialConnection)
                {
                    // First, we get the departure information
                    connection.MoveTo(
                        j.Location.DatabaseId,
                        j.Connection); // The connection will always be hosted by the operator owning the stop position
                    var departure = dbs.LocationOf(connection.DepartureStop);
                    var departureTimed = new TimedLocation(
                        departure,
                        connection.DepartureTime.FromUnixTime(),
                        connection.DepartureDelay);

                    // ... and the trip info (which is saved in the segment section below but not immediatly needed)
                    trip.MoveTo(j.TripId);
                    var vehicleId = trip.GlobalId;
                    trip.Attributes.TryGetValue("headsign", out var headsign);
                    headsign = headsign ?? "";


                    // Now, we walk along the journey to find the end of this segment
                    // In the meanwhile, record intermediate stops
                    var allStations = new List<TimedLocation> {departureTimed};
                    
                    while (true)
                    {
                        if (i + 1 >= parts.Count)
                        {
                            // We have reached the last journey part and thus found the last piece of our journey
                            break;
                        }

                        i++;
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
                        var loc = dbs.LocationOf(parts[i].Location);
                        connection.MoveTo(parts[i].Location.DatabaseId, parts[i - 1].Connection);

                        var tloc = new TimedLocation(loc, parts[i].Time, connection.ArrivalDelay);
                        allStations.Add(tloc);
                    }
                    // At this point, parts[i] is the last part of our journey

                    j = parts[i];
                    
                    connection.MoveTo(j.Location.DatabaseId, j.Connection);
                    var arrival = dbs.LocationOf(connection.ArrivalStop);
                    var arrivalTimed = new TimedLocation(arrival,
                        connection.ArrivalTime, connection.ArrivalDelay);

                    var segment = new Segment(departureTimed, arrivalTimed, vehicleId, headsign, allStations);
                    segments.Add(segment);
                    vehiclesTaken++;
                    continue;
                }


                if (j.SpecialConnection && j.Connection == Journey<T>.OTHERMODE)
                {
                    if (j.Location.Equals(j.PreviousLink.Location))
                    {
                        // Object represent a transfer without moving...
                        // We skip this if the next is an othermode as well
                        continue;
                    }

                    // This is a piece where we walk/cycle/... or some other continuous transportation mode
                    // Lets try to figure out what exactly we are doing

                    var departure = dbs.LocationOf(j.PreviousLink.Location);
                    var departureTimed = new TimedLocation(
                        departure, j.PreviousLink.Time, 0);
                    var arrival = dbs.LocationOf(j.Location);
                    var arrivalTimed = new TimedLocation(
                        arrival, j.Time, 0);

                    List<Coordinate> coordinates = null;

                    if (walkGenerator is FirstLastMilePolicy flm)
                    {
                        walkGenerator = flm.GeneratorFor(j.PreviousLink.Location, j.Location);
                    }

                    if (walkGenerator is OtherModeCacher cacher)
                    {
                        walkGenerator = cacher.Fallback;
                    }

                    if (walkGenerator is OsmTransferGenerator osm)
                    {
                        coordinates = osm.CreateRoute(((float) departure.Lat, (float) departure.Lon),
                            ((float) arrival.Lat, (float) arrival.Lon), out _, out _).Shape.Select(
                            coor => new Coordinate(coor.Latitude, coor.Longitude)).ToList();
                    }

                    var segment = new Segment(
                        departureTimed, arrivalTimed,
                        walkGenerator?.OtherModeIdentifier() ?? "?",
                        coordinates
                    );
                    segments.Add(segment);
                    continue;
                }

                throw new Exception("Case fallthrough");
            }


            return new Models.Journey(segments, vehiclesTaken);
        }

        public static List<Models.Journey> Translate<T>(this State dbs, IEnumerable<Journey<T>> journeys,
            IOtherModeGenerator walkGenerator)
            where T : IJourneyMetric<T>
        {
            var list = new List<Models.Journey>();
            foreach (var j in journeys)
            {
                list.Add(dbs.Translate(j, walkGenerator));
            }

            return list;
        }


        public static Location LocationOf(this State dbs, string globalId)
        {
            var stops = dbs.GetStopsReader(true);
            return !stops.MoveTo(globalId) ? null : new Location(stops);
        }

        private static Location LocationOf(this State dbs, LocationId localId)
        {
            var stops = dbs.GetStopsReader(true);

            if (!stops.MoveTo(localId))
            {
                throw new NullReferenceException("Location " + localId + " not found");
            }

            return new Location(stops);
        }

        internal static LocationSegmentsResult SegmentsForLocation
        (this State dbs,
            string globalId, DateTime time, TimeSpan window)
        {
            if (dbs == null) throw new ArgumentNullException(nameof(dbs));
            var stops = dbs.GetStopsReader(true);
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


                segments.Add(new Segment(departure, arrival, route, headSign, null));
            }

            return new LocationSegmentsResult()
            {
                Location = location,
                Segments = segments.ToArray()
            };
        }
    }
}