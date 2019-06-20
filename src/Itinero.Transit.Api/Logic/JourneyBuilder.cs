using System;
using System.Collections.Generic;
using Itinero.Profiles;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Journey;

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
            uint minimalSearchTimeSeconds = 2 * 60 * 60)
        {
            var stops = State.GlobalState.GetStopsReader();
            stops.MoveTo(from);
            var fromId = stops.Id;
            stops.MoveTo(to);
            var toId = stops.Id;

            var walksGenerator = State.GlobalState.OtherModeBuilder.Create(
                walksGeneratorDescription,
                new List<LocationId> {fromId},
                new List<LocationId> {toId}
            );

            return new RealLifeProfile(
                walksGenerator,
                internalTransferTime,
                searchFactor,
                minimalSearchTimeSeconds);
        }


        public static (List<Journey<TransferMetric>>, DateTime start, DateTime end) BuildJourneys(
            this RealLifeProfile p,
            string from, string to, DateTime? departure,
            DateTime? arrival)
        {
            if (departure == null && arrival == null)
            {
                throw new ArgumentException(
                    "At least one date should be given, either departure time or arrival time (or both)");
            }

            departure = departure?.ToUniversalTime();
            arrival = arrival?.ToUniversalTime();


            var precalculator = State.GlobalState.All()
                .SelectProfile(p)
                .UseOsmLocations()
                .SelectStops(from, to);
            WithTime<TransferMetric> calculator;
            if (departure == null)
            {
                // Departure time is null
                // We calculate one with a latest arrival scan search
                calculator = precalculator.SelectTimeFrame(arrival.Value.AddDays(-1), arrival.Value);
                // This will set the time frame correctly
                calculator
                    .LatestDepartureJourney(tuple =>
                        tuple.journeyStart - p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
            }
            else if (arrival == null)
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, departure.Value.AddDays(1));


                // This will set the time frame correctly + install a filter
                calculator.EarliestArrivalJourney(
                    tuple => tuple.journeyStart + p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
            }
            else
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, arrival.Value);
                // Perform isochrone to speed up 'all journeys'
                calculator.IsochroneFrom();
            }


            return (calculator.AllJourneys(), calculator.Start, calculator.End);
        }
    }
}