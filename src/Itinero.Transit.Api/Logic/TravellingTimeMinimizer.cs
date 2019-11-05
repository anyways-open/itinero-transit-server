using System;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Journey;
using static Itinero.Transit.Journey.Journey<
    Itinero.Transit.Api.Logic.Itinero.Transit.Journey.Metric.TravellingTimeMinimizer>;
// ReSharper disable ImpureMethodCallOnReadonlyValueField

namespace Itinero.Transit.Api.Logic
{
    namespace Itinero.Transit.Journey.Metric
    {
        /// <inheritdoc />
        ///  <summary>
        ///  (aka: The Pragmatic Metric)
        ///  This JourneyMetric will attempt to optimize the journeys in the following way:
        ///  1) The total time walking is minimized
        ///  and iff the same: 
        ///  2) The total time travelling in a vehicle is minimized
        ///  and iff the same:
        ///  3) The smallest transfer time is maximized (e.g. if one journey has a transfer of 2' and one of 6', while another has 3' and 3', the second one is chosen.
        ///  and iff the same and an importance list is given
        ///  4) The biggest stations to transfers are chosen (as bigger station often have better facilities)*
        ///     &gt; Here, the journeys are compared segment by segment. The difference between every station is taken, the one which performs best overall is taken
        ///  This metric is meant to be used _after_ PCS in order to weed out multiple journeys which have an equal performance on total travel time and number of transfers.
        ///  * Once upon a time, I was testing an early implementation. That EAS implementation gave me a transfer of 1h in Angleur
        ///  (which is a small station), while I could have transfered in Liege as well (a big station with lots of facilities).
        ///  The closest food I could find at 19:00 was around one kilometer away.
        ///  </summary>
        public class TravellingTimeMinimizer : IJourneyMetric<TravellingTimeMinimizer>
        {
            private readonly uint _totalTimeWalking;
            private readonly uint _totalTimeInVehicle;
            private readonly uint _smallestTransfer = uint.MaxValue;
            
            public static readonly TravellingTimeMinimizer Factory = new TravellingTimeMinimizer();

            private TravellingTimeMinimizer()
            {
            }

            public TravellingTimeMinimizer(uint totalTimeWalking, uint totalTimeInVehicle, uint smallestTransfer)
            {
                _totalTimeWalking = totalTimeWalking;
                _totalTimeInVehicle = totalTimeInVehicle;
                _smallestTransfer = smallestTransfer;
            }


            public TravellingTimeMinimizer Zero()
            {
                return Factory;
            }

            public TravellingTimeMinimizer Add(Journey<TravellingTimeMinimizer> journey, StopId currentLocation, ulong currentTime, TripId currentTripId,
                bool currentIsSpecial)
            {
                var totalTimeWalking = _totalTimeWalking;
                var totalTimeInVehicle = _totalTimeInVehicle;
                var smallestTransfer = _smallestTransfer;

                var journeyTime = (uint) (journey.ArrivalTime() - journey.DepartureTime());

                if (journey.SpecialConnection && journey.Connection.Equals(OTHERMODE))
                {
                    totalTimeWalking += journeyTime;
                }
                else if (journey.SpecialConnection && journey.Connection.Equals(OTHERMODE))
                {
                    smallestTransfer = Math.Min(smallestTransfer, journeyTime);
                }
                else if (!journey.SpecialConnection)
                {
                    // We simply are travelling in a vehicle
                    totalTimeInVehicle += journeyTime;
                }

                return new TravellingTimeMinimizer(totalTimeWalking, totalTimeInVehicle, smallestTransfer);
            }


          
        }
    }
}