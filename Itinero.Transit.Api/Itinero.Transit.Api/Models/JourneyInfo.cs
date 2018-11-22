// ReSharper disable MemberCanBePrivate.Global

using System.Linq;
using Itinero.Transit.Api.Logic;
using Reminiscence.Collections;

// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api
{
    /// <summary>
    /// An entire Journey. Confusingly named 'connection' in the API
    /// </summary>
    public class JourneyInfo
    {
        /// <summary>
        /// Profile identifier
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Total time of the journey, in seconds
        /// </summary>
        public readonly int Duration;

        public readonly StopInfo Departure, Arrival;

        public readonly ViasInfo Vias;

        public JourneyInfo(int id, int duration, StopInfo departure, StopInfo arrival, List<ViaElement> vias)
        {
            Id = id;
            Duration = duration;
            Departure = departure;
            Arrival = arrival;
            Vias = new ViasInfo(vias);
        }


        /// <summary>
        /// Construct a new JourneyInfo.
        /// Due to the generic type argument, this could not be a constructor.
        /// </summary>
        /// <returns></returns>
        public static JourneyInfo FromJourney<T>(PublicTransportRouter router, int id, Journey<T> journey)
            where T : IJourneyStats<T>
        {
            var connections = journey.AllConnections();

            var duration = (int)
                (connections.Last().ArrivalTime() - connections[1].DepartureTime()).TotalSeconds;

            // We take the second element in the list, as the  first element is but a 'genesisConnection'
            var departure = new StopInfo(router, connections[1], departure: true);
            var arrival = new StopInfo(router, connections.Last(), departure: false);

            var viaEls = new List<ViaElement>();

            //Search for transfers
            for (var i = 2; i < connections.Count - 1; i++)
            {
                var conn = connections[i];

                if (!(conn is InternalTransfer)) continue;


                var viaEl =
                    new ViaElement(router, viaEls.Count,
                        connections[i - 1], connections[i + 1]);

                viaEls.Add(viaEl);
            }

            return new JourneyInfo(id, duration, departure, arrival, viaEls);
        }


        public class ViasInfo
        {
            public readonly int Number;
            public readonly List<ViaElement> Via;

            public ViasInfo(List<ViaElement> via)
            {
                Number = via.Count;
                Via = via;
            }
        }
    }
}