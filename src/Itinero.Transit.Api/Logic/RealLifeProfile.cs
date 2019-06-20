using System;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Api.Logic
{
    public class RealLifeProfile : Profile<TransferMetric>
    {
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
        /// <param name="maxWalkingDistance">How far the traveller at most wants to walk between multiple stops (e.g. from bus to train)</param>
        /// <param name="walkingspeedMetersPerSecond">How fast the traveller walks (in meter per second)</param>
        /// <param name="walksGenerator">The walks generator, probably a FirstLastMile</param>
        /// <param name="searchFactor">If an initial journey of duration n is found, the search window will be 'factor * n'</param>
        /// <param name="minimalSearchTimeSeconds">Search journeys in a timewindow of at least this length (in seconds)</param>
        public RealLifeProfile(
            IOtherModeGenerator walksGenerator,
            uint internalTransferTime = 180,
            double searchFactor = 2.5, 
            uint minimalSearchTimeSeconds = 2 * 60 * 60) :
            this(
                new InternalTransferGenerator(internalTransferTime),
                walksGenerator,
                DefaultSearchLengthSearcher(searchFactor, TimeSpan.FromSeconds(minimalSearchTimeSeconds)))
        {
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