using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
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
        public static List<Journey<TransferStats>> ApplyTransferPenalty(
            this RealLifeProfile profile, List<Journey<TransferStats>> journeys)
        {
            var penaltyInSeconds = profile.TransferPenalty;
            if (penaltyInSeconds == 0)
            {
                return journeys;
            }
            var sortedByArrivalDesc = journeys.OrderBy(j => 0 - j.Time);

            var result = new List<Journey<TransferStats>>();
            var lastTimeWithPenalty = ulong.MaxValue;
            Journey<TransferStats> last = null;

            foreach (var journey in sortedByArrivalDesc)
            {
                if (last != null && last.Root.Time != journey.Root.Time)
                {
                    continue;
                }

                // The arrival time gets a penalty per transfer
                var arrivalWithPenalty = journey.Time + journey.Stats.NumberOfTransfers * penaltyInSeconds;

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

        public static List<Journey<TransferStats>> BuildJourneys(
            this RealLifeProfile p,
            string from, string to, DateTime? departure,
            DateTime? arrival)
        {
            if (departure == null && arrival == null)
            {
                throw new ArgumentException(
                    "At least one date should be given, either departure time or arrival time (or both)");
            }

            var snapshot = State.TransitDb.Latest;
            var fromId = snapshot.FindStop(from);
            var toId = snapshot.FindStop(to);



            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                var lasJ = snapshot.CalculateLatestDeparture(p, from, to, arrival.Value - TimeSpan.FromDays(1), arrival.Value);
                arrival = lasJ.Time.FromUnixTime();
                departure = arrival - p.SearchLengthCalculator(lasJ.Root.Time.FromUnixTime(),
                                arrival.Value);
            }

            /*
             * The following line calculates an earliest arrival based on the departure time.
             * The earliest arrival is transformed into a filter which will optimize the PCS later on
             *
             * Note that the departure time might be put a little later by this method
             * Note that this method guarantees to return an arrival time, either the given one or one obeying the minimum search span as defined in the realProfile
             * 
             */
            var (departureNotNull, arrivalNotNull, filter) = snapshot.FindArrivalTime(
                p, departure.Value, arrival, fromId, toId);


            var journeys = snapshot.CalculateJourneys(p, fromId, toId, departureNotNull, arrivalNotNull, filter);


            return journeys;
        }


        /// <summary>
        /// If no arrival time is given, performs EAS to calculate an estimated latest arrival time.
        ///
        /// Note that: if both dates are already known, both are returned unchanged. EAS will still be performed though for optimization the PCS in a later phase.
        ///
        /// If arrival date is not known, an arrival time is calculated based on the RealProfile preferences
        /// One minute of padding before and after is added.
        /// 
        /// </summary>
        /// <returns></returns>
        private static (DateTime deparute, DateTime arrival, IConnectionFilter filter)
            FindArrivalTime(
                this TransitDb.TransitDbSnapShot snapshot,
                RealLifeProfile profile,
                DateTime departure, DateTime? arrival,
                LocationId from, LocationId to)
        {
            if (arrival != null)
            {
                var easJ = snapshot.CalculateEarliestArrival(profile, from, to, departure, arrival.Value,
                    out var filter);
                return (easJ.Root.Time.FromUnixTime(),
                    arrival.Value, filter);
            }
            else
            {
                var easJ = snapshot.CalculateEarliestArrival
                (profile, from, to, out var filter, departure,
                    (start, end) => start + profile.SearchLengthCalculator(start, end));
                // THe actual search end time is calculated based on the length of the earliest arrival journey
                // and what the function in the profile makes from it
                var depTime = easJ.Root.Time.FromUnixTime();
                var endTime = depTime +
                              profile.SearchLengthCalculator(easJ.Root.Time.FromUnixTime(), easJ.Time.FromUnixTime());
                return (depTime, endTime, filter);
            }
        }
    }
}