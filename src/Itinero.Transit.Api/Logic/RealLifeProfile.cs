using System;
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Data;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;

namespace Itinero.Transit.Api.Logic
{
    public class RealLifeProfile : Profile<TransferMetric>
    {
        private readonly uint _maxNumberOfTransfers;
        private readonly ChangeableMaxNumberOfTransferFilter _filter;

        /// <summary>
        /// The timespan in which a search should be performed, depending on a single EAS/LAS journey
        /// </summary>
        public Func<DateTime, DateTime, TimeSpan> SearchLengthCalculator { get; }

        public readonly OperatorSet OperatorSet;


        public RealLifeProfile(
            OperatorSet operatorSet,
            IOtherModeGenerator internalTransferGenerator,
            IOtherModeGenerator walksGenerator,
            bool allowCancelled,
            uint maxNumberOfTransfers,
            Func<DateTime, DateTime, TimeSpan> searchLengthCalculator
        ) :
            base(
                internalTransferGenerator,
                walksGenerator,
                TransferMetric.Factory,
                TransferMetric.ParetoCompare,
                allowCancelled ? null : new CancelledConnectionFilter(),
                new ChangeableMaxNumberOfTransferFilter(uint.MaxValue))
        {
            OperatorSet = operatorSet;
            _filter = (ChangeableMaxNumberOfTransferFilter) JourneyFilter;
            _maxNumberOfTransfers = maxNumberOfTransfers;
            SearchLengthCalculator = searchLengthCalculator;
        }

        public void ApplyMaxNumberOfTransfers()
        {
            _filter.MaxNumberOfTransfers = _maxNumberOfTransfers;
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
    }
}