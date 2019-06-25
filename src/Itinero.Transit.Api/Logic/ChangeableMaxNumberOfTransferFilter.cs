using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Filter;
using Itinero.Transit.Journey.Metric;

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