using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;

namespace Itinero.Transit.Api.Logic
{
    public class RealLifeProfile : Profile<TransferStats>
    {
        
        /// <summary>
        /// The timespan in which a search should be performed, depending on a single EAS/LAS journey
        /// </summary>
        public Func<DateTime, DateTime, TimeSpan> SearchLengthCalculator { get; private set; }


        public RealLifeProfile(IOtherModeGenerator internalTransferGenerator, IOtherModeGenerator walksGenerator,
            Func<DateTime, DateTime, TimeSpan> searchLengthCalculator) :
            base(
                internalTransferGenerator, walksGenerator, TransferStats.Factory, TransferStats.ProfileCompare)
        {
            SearchLengthCalculator = searchLengthCalculator;
        }


        public static Func<DateTime, DateTime, TimeSpan> DefaultSearchLengthSearcher(
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


        public RealLifeProfile(uint internalTransferTime = 180, int maxDistance = 500, float speed = 1.4f,
            double searchFactor = 2.5, uint minimalSearchTimeSeconds = 2*60*60) :
            this(
                new InternalTransferGenerator(internalTransferTime),
                new CrowsFlightTransferGenerator(maxDistance, speed),
                DefaultSearchLengthSearcher(searchFactor, TimeSpan.FromSeconds(minimalSearchTimeSeconds)))
        {
        }
    }
}