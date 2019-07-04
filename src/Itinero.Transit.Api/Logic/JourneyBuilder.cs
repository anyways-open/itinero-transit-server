using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Journey;
using Itinero.Transit.Utils;

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
            var stops = State.GlobalState.GetStopsReader(0);

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


            var precalculator =
                State.GlobalState.All()
                    .SelectProfile(p)
                    .SetStopsReader(State.GlobalState.GetStopsReader((uint) (p.WalksGenerator?.Range() ?? 0)))
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
                return (new List<Journey<TransferMetric>> {latest},
                    latest.Root.Time.FromUnixTime(), latest.Time.FromUnixTime());
            }
            else // if (arrival == null)
            {
                calculator = precalculator.SelectTimeFrame(departure.Value, departure.Value.AddDays(1));


                // This will set the time frame correctly + install a filter
                var earliestArrivalJourney = calculator.EarliestArrivalJourney(
                    tuple => tuple.journeyStart + p.SearchLengthCalculator(tuple.journeyStart, tuple.journeyEnd));
                return (new List<Journey<TransferMetric>> {earliestArrivalJourney},
                    earliestArrivalJourney.Root.Time.FromUnixTime(), earliestArrivalJourney.Time.FromUnixTime());
            }
        }

/*     /* else
      {
          calculator = precalculator.SelectTimeFrame(departure.Value, arrival.Value);
          // Perform isochrone to speed up 'all journeys'
          calculator.IsochroneFrom();
      }
/*

      // We lower the max number of transfers to speed up calculations
     // p.ApplyMaxNumberOfTransfers();


    //  return (calculator.AllJourneys(), calculator.Start, calculator.End);*/
    }
}