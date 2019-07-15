using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Profiles;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Microsoft.EntityFrameworkCore.Internal;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// This class builds a collection of journeys for the traveller, based on heuristics and what works well in practice
    /// </summary>
    public static class JourneyBuilder
    {
        public static RealLifeProfile CreateProfile(string @from, string to,
            string walksGeneratorDescription,
            uint internalTransferTime = 180,
            double searchFactor = 2.5,
            uint minimalSearchTimeSeconds = 2 * 60 * 60,
            bool allowCancelled = false,
            uint maxNumberOfTransfers = uint.MaxValue
        )
        {
            var stops = State.GlobalState.GetStopsReader(true);

            stops.MoveTo(from);
            var fromId = stops.Id;
            stops.MoveTo(to);
            var toId = stops.Id;


            var walksGenerator = State.GlobalState.OtherModeBuilder.Create(
                walksGeneratorDescription,
                new List<LocationId> {fromId},
                new List<LocationId> {toId}
            );


            var internalTransferGenerator = new InternalTransferGenerator(internalTransferTime);

            var searchFunction =
                RealLifeProfile.DefaultSearchLengthSearcher(searchFactor,
                    TimeSpan.FromSeconds(minimalSearchTimeSeconds));

            return new RealLifeProfile(
                internalTransferGenerator,
                walksGenerator,
                allowCancelled,
                maxNumberOfTransfers,
                searchFunction
            );
        }


        private static void DetectFirstMileWalks<T>(
            this Profile<T> p,
            IStopsReader stops, IStop stop, uint osmIndex) where T : IJourneyMetric<T>
        {
            if (stop.Id.DatabaseId != osmIndex)
            {
                // The location is already on the Public Transport network
                // We don't need to walk
                // and thus don't need to check that a start walk exists
                return;
            }

            var inRange = stops.LocationsInRange(stop.Latitude, stop.Longitude, p.WalksGenerator.Range()).ToList();
            inRange.Remove(stop);
            if (inRange == null || !inRange.Any())
            {
                throw new ArgumentException(
                    $"Could not find a station that is range from {stop.GlobalId} within {p.WalksGenerator.Range()}m. This is in crows flight, try increasing the range of your walksGenerator");
            }

            var foundRoutes = p.WalksGenerator.TimesBetween(stop, inRange);

            if (foundRoutes == null || !foundRoutes.Any())
            {
                var w = p.WalksGenerator;

                var errors = new List<string>();
                foreach (var stp in inRange)
                {
                    if (w is OsmTransferGenerator osm)
                    {
                        osm.CreateRoute((stop.Latitude, stop.Longitude), (stp.Latitude, stp.Longitude), out _,
                            out var errMessage);
                        errors.Add($"Could not reach {stp.GlobalId}: {errMessage}");
                    }
                }

                var allErrs = string.Join("\n ", errors);

                throw new ArgumentException(
                    $"Could not find a route over OSM towards/from the departure or end station.\n Station is {stop.GlobalId}\n {allErrs}");
            }
        }

        public static (List<Journey<TransferMetric>>, DateTime start, DateTime end) BuildJourneys(
            this RealLifeProfile p,
            string from, string to, DateTime? departure,
            DateTime? arrival,
            bool multipleOptions)
        {
            if (departure == null && arrival == null)
            {
                throw new ArgumentException(
                    "At least one date should be given, either departure time or arrival time (or both)");
            }

            departure = departure?.ToUniversalTime();
            arrival = arrival?.ToUniversalTime();

            var reader = State.GlobalState.GetStopsReader(false);
            var osmIndex = reader.DatabaseIndexes().Max() + 1u;

            var stopsReader =
                StopsReaderAggregator.CreateFrom(new List<IStopsReader>
                {
                    reader,
                    new OsmLocationStopReader(osmIndex, true),
                }); // We don't cache here - only case cache will be missed is around the new stop locations

            // Calculate the first and last miles, in order to
            // 1) Detect impossible routes
            // 2) cache them

            stopsReader.MoveTo(from);
            var fromStop = new Stop(stopsReader);


            stopsReader.MoveTo(to);
            var toStop = new Stop(stopsReader);
            
            p.DetectFirstMileWalks(stopsReader, fromStop, osmIndex);
            p.DetectFirstMileWalks(stopsReader, toStop, osmIndex);


            var precalculator =
                State.GlobalState.All()
                    .SelectProfile(p)
                    .SetStopsReader(() => stopsReader)
                    .UseOsmLocations()
                    .SelectStops(from, to);
            WithTime<TransferMetric> calculator;
            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                calculator = precalculator.SelectTimeFrame(arrival.Value.AddDays(-1), arrival.Value);
                // This will set the time frame correctly
                var latest = calculator
                    .LatestDepartureJourney(tuple =>
                        tuple.journeyStart - p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
                if (!multipleOptions)
                {
                    return (new List<Journey<TransferMetric>> {latest},
                        latest.Root.Time.FromUnixTime(), latest.Time.FromUnixTime());
                }
            }
            else if (arrival == null || !multipleOptions)
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, departure.Value.AddDays(1));


                // This will set the time frame correctly + install a filter
                var earliestArrivalJourney = calculator.EarliestArrivalJourney(
                    tuple => tuple.journeyStart + p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
                if (earliestArrivalJourney == null)
                {
                    return (new List<Journey<TransferMetric>>(),
                        DateTime.MaxValue, DateTime.MinValue);
                }

                if (!multipleOptions)
                {
                    return (new List<Journey<TransferMetric>> {earliestArrivalJourney},
                        earliestArrivalJourney.Root.Time.FromUnixTime(), earliestArrivalJourney.Time.FromUnixTime());
                }
            }
            else
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, arrival.Value);
                // Perform isochrone to speed up 'all journeys'
                calculator.IsochroneFrom();
            }

            // We lower the max number of transfers to speed up calculations
            p.ApplyMaxNumberOfTransfers();


            return (calculator.AllJourneys(), calculator.Start, calculator.End);
        }
    }
}