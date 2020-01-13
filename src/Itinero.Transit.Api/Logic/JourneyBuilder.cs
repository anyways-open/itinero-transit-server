using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// This class builds a collection of journeys for the traveller, based on heuristics and what works well in practice
    /// </summary>
    public static class JourneyBuilder
    {
        private static Dictionary<Stop, uint> DetectFirstMileWalks<T>(
            this Profile<T> p,
            IStopsDb stops, Stop stop, bool isLastMile, string name) where T : IJourneyMetric<T>
        {
            if (p.WalksGenerator.Range() == 0)
            {
                // We can't walk with the current settings
                return null;
            }

            var inRange = stops.GetInRange(stop, p.WalksGenerator.Range()).ToList();
            if (!inRange.Any()
                || inRange.Count == 1 && inRange[0].Equals(stop))
            {
                return null;
            }

            var foundRoutes = isLastMile
                ? p.WalksGenerator.TimesBetween(inRange, stop)
                : p.WalksGenerator.TimesBetween(stop, inRange);


            Log.Verbose($"Found {foundRoutes.Count} direct routes");
            return foundRoutes;
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
        /// <param name="logMessage"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static (List<Journey<TransferMetric>>, Segment directJourneyTimeNeeded, DateTime start, DateTime end)
            BuildJourneys(this RealLifeProfile p,
                string @from, string to, DateTime? departure,
                DateTime? arrival,
                bool multipleOptions, Dictionary<string, string> logMessage)
        {
            if (departure == null && arrival == null)
            {
                throw new ArgumentException(
                    "At least one date should be given, either departure time or arrival time (or both)");
            }

            departure = departure?.ToUniversalTime();
            arrival = arrival?.ToUniversalTime();

            var stopsDb = p.OperatorSet.GetStops()
                .AddOsmReader(new[] {from, to});
            stopsDb = stopsDb.UseCache(); // We build an extra layer of caching, which is discarded after this call


            // Calculate the first and last miles, in order to
            // 1) Detect impossible routes
            // 2) cache them

            var fromStop = stopsDb.Get(from);

            var toStop = stopsDb.Get(to);

            p.DetectFirstMileWalks(stopsDb, fromStop, false, "departure");
            p.DetectFirstMileWalks(stopsDb, toStop, true, "arrival");

            var precalculator =
                p.OperatorSet.All()
                    .SelectProfile(p)
                    .SetStopsDb(stopsDb)
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

                logMessage.Add("earliestArrivalJourney:departure",
                    earliestArrivalJourney.Root.Time.FromUnixTime().ToString("s"));
                logMessage.Add("earliestArrivalJourney:arrival",
                    earliestArrivalJourney.Time.FromUnixTime().ToString("s"));
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
            //  p.ApplyMaxNumberOfTransfers();

            logMessage.Add("searchTime:pcs:start", calculator.Start.ToString("s"));
            logMessage.Add("searchTime:pcs:end", calculator.End.ToString("s"));


            return (calculator.CalculateAllJourneys(), directRoute, calculator.Start, calculator.End);
        }

        private static Segment CalculateDirectRoute(RealLifeProfile p,
            WithLocation<TransferMetric> precalculator,
            Stop fromStop, Stop toStop)
        {
            var directJourneyTime = uint.MaxValue;

            if (p.WalksGenerator.Range() <
                DistanceEstimate.DistanceEstimateInMeter(
                    (fromStop.Longitude, fromStop.Latitude),
                    (toStop.Longitude, toStop.Latitude)))
            {
                return null;
            }

            precalculator.CalculateDirectJourney()
                ?.TryGetValue((fromStop, toStop), out directJourneyTime);

            if (directJourneyTime == uint.MaxValue)
            {
                return null;
            }

            var (coordinates, generator, license) =
                p.WalksGenerator.GetCoordinatesFor(fromStop, toStop);

            var departure = new TimedLocation(new Location(fromStop), null, 0);
            var arrival = new TimedLocation(new Location(toStop), null, 0);
            return new Segment(departure, arrival, generator, coordinates, directJourneyTime, license);
        }

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
            var stops = operatorSet.GetStops().AddOsmReader();

            var fromStop = stops.Get(from);
            var toStop = stops.Get(to);


            var walksGenerator = State.GlobalState.OtherModeBuilder.Create(
                walksGeneratorDescription,
                new List<Stop> {fromStop},
                new List<Stop> {toStop}
            );


            var internalTransferGenerator = new InternalTransferGenerator(internalTransferTime);

            var maxDistance = uint.MaxValue;
            foreach (var op in operatorSet.Operators)
            {
                maxDistance = Math.Min(maxDistance, op.MaxSearch);
            }

            if (walksGenerator.Range() > maxDistance)
            {
                throw new ArgumentException(
                    $"Search range too high: with the chosen operators, at most {maxDistance}m is allowed");
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
    }
}