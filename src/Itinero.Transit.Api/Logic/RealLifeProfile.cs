using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Api.Logic
{
    public class RealLifeProfile : Profile<TransferMetric>
    {
        public uint TransferPenalty { get; }

        /// <summary>
        /// The timespan in which a search should be performed, depending on a single EAS/LAS journey
        /// </summary>
        public Func<DateTime, DateTime, TimeSpan> SearchLengthCalculator { get; }


        public RealLifeProfile(IOtherModeGenerator internalTransferGenerator, IOtherModeGenerator walksGenerator,
            Func<DateTime, DateTime, TimeSpan> searchLengthCalculator) :
            base(
                internalTransferGenerator, walksGenerator, TransferMetric.Factory, TransferMetric.ProfileTransferCompare)
        {
            SearchLengthCalculator = searchLengthCalculator;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="internalTransferTime">How long the traveller needs to go from one train to another</param>
        /// <param name="transferPenalty">If two journeys depart at the same moment, and one is slightly faster at the cost of transfers, don't show the train with transfers if it is only 'penalty*number of transfers' faster</param>
        /// <param name="maxWalkingDistance">How far the traveller at most wants to walk between multiple stops (e.g. from bus to train)</param>
        /// <param name="walkingspeedMetersPerSecond">How fast the traveller walks (in meter per second)</param>
        /// <param name="searchFactor">If an initial journey of duration n is found, the search window will be 'factor * n'</param>
        /// <param name="minimalSearchTimeSeconds">Search journeys in a timewindow of at least this length (in seconds)</param>
        public RealLifeProfile(uint internalTransferTime = 180,
            uint transferPenalty = 300,
            int maxWalkingDistance = 500, float walkingspeedMetersPerSecond = 1.4f,
            double searchFactor = 2.5, uint minimalSearchTimeSeconds = 2 * 60 * 60) :
            this(
                new InternalTransferGenerator(internalTransferTime),
                new CrowsFlightTransferGenerator(maxWalkingDistance, walkingspeedMetersPerSecond),
                DefaultSearchLengthSearcher(searchFactor, TimeSpan.FromSeconds(minimalSearchTimeSeconds)))
        {
            TransferPenalty = transferPenalty;
        }


        private static Func<DateTime, DateTime, TimeSpan> DefaultSearchLengthSearcher(
            double factor, TimeSpan minimumTime)
        {
            return (start, end) =>
            {
                var diff = (end - start) * factor;

                if (diff < minimumTime)
                {
                    diff = minimumTime;
                }

                return diff;
            };
        }
    }
}