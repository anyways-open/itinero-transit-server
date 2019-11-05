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
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// This class builds a collection of journeys for the traveller, based on heuristics and what works well in practice
    /// </summary>
    public static class JourneyBuilder
    {
        public static RealLifeProfile CreateProfile(
            this OperatorSet operatorSet,
            string from, string to,
            string walksGeneratorDescription,
            uint internalTransferTime = 180,
            double searchFactor = 2.5,
            uint minimalSearchTimeSeconds = 2 * 60 * 60,
            bool allowCancelled = false,
            uint maxNumberOfTransfers = uint.MaxValue
        )
        {
            var stops = operatorSet.GetStopsReader().AddOsmReader();

            stops.MoveTo(from);
            var fromId = stops.Id;
            stops.MoveTo(to);
            var toId = stops.Id;


            var walksGenerator = State.GlobalState.OtherModeBuilder.Create(
                walksGeneratorDescription,
                new List<StopId> {fromId},
                new List<StopId> {toId}
            );


            var internalTransferGenerator = new InternalTransferGenerator(internalTransferTime);
            
            var maxDistance = uint.MaxValue;
            foreach (var op in operatorSet.Operators)
            {
                maxDistance = Math.Min(maxDistance, op.MaxSearch);
            }

            if (walksGenerator.Range() > maxDistance)
            {
                throw new ArgumentException($"Search range too high: with the chosen operators, at most {maxDistance}m is allowed");
            }

            var searchFunction =
                RealLifeProfile.DefaultSearchLengthSearcher(searchFactor,
                    TimeSpan.FromSeconds(minimalSearchTimeSeconds));

            return new RealLifeProfile(
                operatorSet,
                internalTransferGenerator,
                walksGenerator,
                allowCancelled,
                maxNumberOfTransfers,
                searchFunction
            );
        }


        private static void DetectFirstMileWalks<T>(
            this Profile<T> p,
            IStopsReader stops, Stop stop, uint osmIndex, bool isLastMile, string name) where T : IJourneyMetric<T>
        {
            var failIfNoneFound = stop.Id.DatabaseId == osmIndex;


            if (stop.Id.DatabaseId != osmIndex)
            {
                // The location is already on the Public Transport network
                // We don't need to walk
                // and thus don't need to check that a start walk exists
                return;
            }

            if (p.WalksGenerator.Range() == 0)
            {
                // We can't walk with the current settings
                return;
            }

            var inRange = stops.StopsAround(stop, p.WalksGenerator.Range()).ToList();
            if (inRange == null
                || !inRange.Any()
                || inRange.Count == 1 && inRange[0].Id.Equals(stop.Id))
            {
                if (!failIfNoneFound)
                {
                    return;
                }

                throw new ArgumentException(
                    $"Could not find a station that is in range from the {name}-location {stop.GlobalId} within {p.WalksGenerator.Range()}m. This range is calculated 'as the  crows fly', try increasing the range of your walksGenerator");
            }

            var foundRoutes = isLastMile
                ? p.WalksGenerator.TimesBetween(inRange, stop)
                : p.WalksGenerator.TimesBetween(stop, inRange);


            if (!failIfNoneFound)
            {
                return;
            }

            if (foundRoutes == null)
            {
                throw CreateException(p, stop, isLastMile, name, inRange);
            }

            if (!foundRoutes.Any())
            {
                throw CreateException(p, stop, isLastMile, name, inRange);
            }

            foreach (var (_, distance) in foundRoutes)
            {
                if (distance != uint.MaxValue)
                {
                    return;
                }
            }

            throw CreateException(p, stop, isLastMile, name, inRange);
        }

        private static ArgumentException CreateException<T>(Profile<T> p, IStop stop, bool isLastMile,
            string name, IReadOnlyCollection<Stop> inRange)
            where T : IJourneyMetric<T>
        {
            var w = p.WalksGenerator;


            var errors = new List<string>();
            foreach (var stp in inRange)
            {
                var gen = w.GetSource(stop.Id, stp.Id);
                if (isLastMile)
                {
                    gen = w.GetSource(stp.Id, stop.Id);
                }

                var errorMessage =
                    $"A route from/to {stp} should have been calculated with {gen.OtherModeIdentifier()}";

                if (gen is OsmTransferGenerator osm)
                {
                    // THIS IS ONLY THE ERROR CASE
                    // NO, this isn't cached, I know that - it doesn't matter
                    osm.CreateRoute((stop.Latitude, stop.Longitude), (stp.Latitude, stp.Longitude), out _,
                        out var errMessage);
                    errorMessage += " but it said " + errMessage;
                }

                if (gen is CrowsFlightTransferGenerator)
                {
                    errorMessage += " 'Too Far'";
                }

                errors.Add(errorMessage);
            }

            var allErrs = string.Join("\n ", errors);

            return new ArgumentException(
                $"Could not find a route towards/from the {name}-location.\nThe used generator is {w.OtherModeIdentifier()}\n{inRange.Count} stations in range are known\n The location we couldn't reach is {stop.GlobalId}\n {allErrs}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="departure"></param>
        /// <param name="arrival"></param>
        /// <param name="multipleOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static (List<Journey<TransferMetric>>, Segment directJourneyTimeNeeded, DateTime start, DateTime end)
            BuildJourneys(
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

            var reader = p.OperatorSet.GetStopsReader();
            var osmIndex = reader.DatabaseIndexes().Max() + 1u;

            var stopsReader = StopsReaderAggregator.CreateFrom(new List<IStopsReader>
            {
                reader,
                new OsmLocationStopReader(osmIndex, true),
            }).UseCache(); // We cache here only for this request- only case cache will be missed is around the new stop locations

            // Calculate the first and last miles, in order to
            // 1) Detect impossible routes
            // 2) cache them

            stopsReader.MoveTo(from);
            var fromStop = new Stop(stopsReader);

            stopsReader.MoveTo(to);
            var toStop = new Stop(stopsReader);

            p.DetectFirstMileWalks(stopsReader, fromStop, osmIndex, false, "departure");
            p.DetectFirstMileWalks(stopsReader, toStop, osmIndex, true, "arrival");

            var precalculator =
                p.OperatorSet.All()
                    .SelectProfile(p)
                    .SetStopsReader(stopsReader)
                    .SelectStops(from, to);

            var directRoute = CalculateDirectRoute(p, precalculator, fromStop, toStop);

            WithTime<TransferMetric> calculator;
            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                calculator = precalculator.SelectTimeFrame(arrival.Value.AddDays(-1), arrival.Value);
                // This will set the time frame correctly
                var latest = calculator
                    .CalculateLatestDepartureJourney(tuple =>
                        tuple.journeyStart - p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
                if (!multipleOptions)
                {
                    return (new List<Journey<TransferMetric>> {latest}, directRoute,
                        latest.Root.Time.FromUnixTime(), latest.Time.FromUnixTime());
                }
            }
            else if (arrival == null || !multipleOptions)
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, departure.Value.AddDays(1));


                // We do an earliest arrival search in a timewindow of departure time -> latest arrival time (eventually with arrival + 1 day)
                // This scan is extended for some time, in order to have both
                // - the automatically calculated latest arrival time
                // - an isochrone line in order to optimize later on
                var earliestArrivalJourney = calculator.CalculateEarliestArrivalJourney(
                    tuple => tuple.journeyStart + p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
                if (earliestArrivalJourney == null)
                {
                    return (new List<Journey<TransferMetric>>(), directRoute,
                        DateTime.MaxValue, DateTime.MinValue);
                }

                if (!multipleOptions)
                {
                    return (new List<Journey<TransferMetric>> {earliestArrivalJourney}, directRoute,
                        earliestArrivalJourney.Root.Time.FromUnixTime(), earliestArrivalJourney.Time.FromUnixTime());
                }
            }
            else
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, arrival.Value);
                // Perform isochrone to speed up 'all journeys'
                calculator.CalculateIsochroneFrom();
            }

            // We lower the max number of transfers to speed up calculations
            p.ApplyMaxNumberOfTransfers();


            return (calculator.CalculateAllJourneys(), directRoute, calculator.Start, calculator.End);
        }

        private static Segment CalculateDirectRoute(RealLifeProfile p,
            WithLocation<TransferMetric> precalculator,
            Stop fromStop, Stop toStop)
        {
            var directJourneyTime = uint.MaxValue;

            if (DistanceEstimate.DistanceEstimateInMeter(
                    fromStop.Latitude, fromStop.Longitude,
                    toStop.Latitude, toStop.Longitude) > p.WalksGenerator.Range())
            {
                return null;
            }

            precalculator.CalculateDirectJourney()
                ?.TryGetValue((fromStop.Id, toStop.Id), out directJourneyTime);

            if (directJourneyTime == uint.MaxValue)
            {
                return null;
            }

            var (coordinates, generator, license) =
                JourneyTranslator.GetCoordinatesFor(p.WalksGenerator, fromStop, toStop);

            var departure = new TimedLocation(new Location(fromStop), null, 0);
            var arrival = new TimedLocation(new Location(toStop), null, 0);
            return new Segment(departure, arrival, generator, coordinates, directJourneyTime, license);
        }
    }
}