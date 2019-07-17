
using Itinero.Transit.Algorithms.Filter;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Journey;

namespace Itinero.Transit.Api.Logic
{
    public class ChangeableMaxNumberOfTransferFilter : IJourneyFilter<TransferMetric>
    {
        /// <summary>
        /// Change this one if needed
        /// </summary>
        public uint MaxNumberOfTransfers;

        public ChangeableMaxNumberOfTransferFilter(uint numberOfTransfers)
        {
            MaxNumberOfTransfers = numberOfTransfers;
        }
        
        public bool CanBeTaken(Journey<TransferMetric> journey)
        {
            return journey.Metric.NumberOfTransfers <= MaxNumberOfTransfers;
        }

        public bool CanBeTakenBackwards(Journey<TransferMetric> journey)
        {
            return journey.Metric.NumberOfTransfers <= MaxNumberOfTransfers;
        }
    }
}