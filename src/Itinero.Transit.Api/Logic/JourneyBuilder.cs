using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// This class builds a collection of journeys for the traveller, based on heuristics and what works well in practice
    /// </summary>
    public static class JourneyBuilder
    {
        /// <summary>
        /// This function creates a new list which contains all the journeys from the given list,
        /// but without journeys which are just slightly faster at the cost of transfers.
        ///
        /// E.g. if a journey X arrives a few minutes earlier then a journey Y, but X has one more transfer, X is removed.
        /// Note that we still keep X even if it would depart a few minutes later - a traveller missing the 'direct' train will be glad to know the indirect one a few minutes later!
        ///
        /// We thus only apply this if the departure times are the same
        /// 
        /// X: arrives at 10:00, one transfer -> Penalty time: 10:10
        /// Y: arrives at 10:01, no transfers
        /// Z: arrives at 10:02, one transfers -> Penalty time: 10:12
        /// </summary>
        /// <returns></returns>
        public static List<Journey<TransferMetric>> ApplyTransferPenalty(
            this RealLifeProfile profile, List<Journey<TransferMetric>> journeys)
        {
            var penaltyInSeconds = profile.TransferPenalty;
            if (penaltyInSeconds == 0)
            {
                return journeys;
            }

            var sortedByArrivalDesc = journeys.OrderBy(j => 0 - j.Time);

            var result = new List<Journey<TransferMetric>>();
            var lastTimeWithPenalty = ulong.MaxValue;
            Journey<TransferMetric> last = null;

            foreach (var journey in sortedByArrivalDesc)
            {
                if (last != null && last.Root.Time != journey.Root.Time)
                {
                    continue;
                }

                // The arrival time gets a penalty per transfer
                var arrivalWithPenalty = journey.Time + journey.Metric.NumberOfTransfers * penaltyInSeconds;

                if (lastTimeWithPenalty < arrivalWithPenalty)
                {
                    continue;
                }

                result.Add(journey);
                last = journey;
                lastTimeWithPenalty = arrivalWithPenalty;
            }

            return result;
        }

        public static List<Journey<TransferMetric>> BuildJourneys(
            this RealLifeProfile p,
            string from, string to, DateTime? departure,
            DateTime? arrival)
        {
            if (departure == null && arrival == null)
            {
                throw new ArgumentException(
                    "At least one date should be given, either departure time or arrival time (or both)");
            }

            
            
            WithTime<TransferMetric> calculator;
            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                calculator = State.TransitDbs()
                    .SelectProfile(p)
                    .SelectStops(from, to)
                    .SelectTimeFrame(arrival.Value.AddDays(-1), arrival.Value);
                // This will set the timeframe correctly
                calculator
                    .LatestDepartureJourney(tuple => tuple.journeyStart - p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
            }
            else if(arrival == null)
            {
                calculator = State.TransitDbs()
                    .SelectProfile(p)
                    .SelectStops(from, to)
                    .SelectTimeFrame(departure.Value, departure.Value.AddDays(1));


                // This will set the time frame correctly + install a filter
                calculator.EarliestArrivalJourney(
                    tuple => tuple.journeyStart + p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
               
            }
            else
            {
                 calculator = State.TransitDbs()
                    .SelectProfile(p)
                    .SelectStops(from, to)
                    .SelectTimeFrame(departure.Value, arrival.Value);
                // Perform isochrone to speed up 'all journeys'
                calculator.IsochroneFrom();

            }


            return calculator.AllJourneys();
        }
    }
}