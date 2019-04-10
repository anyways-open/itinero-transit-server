using System;
using System.Collections.Generic;
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
        public static List<Journey<TransferStats>> BuildJourneys(string from, string to, DateTime? departure,
            DateTime? arrival,
            uint internalTransferTime)
        {
            if (departure == null && arrival == null)
            {
                throw new ArgumentException(
                    "At least one date should be given, either departure time or arrival time (or both)");
            }

            var snapshot = State.TransitDb.Latest;
            var fromId = snapshot.FindStop(from);
            var toId = snapshot.FindStop(to);


            var p = new RealLifeProfile(internalTransferTime);


            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                var lasJ = snapshot.CalculateLatestDeparture(p, from, to, DateTime.MinValue, arrival.Value);
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