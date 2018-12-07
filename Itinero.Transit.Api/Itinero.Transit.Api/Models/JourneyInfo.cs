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
    public class JourneyInfo<T> where T : IJourneyStats<T>
    {
        /// <summary>
        /// Profile identifier
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Total time of the journey, in seconds
        /// </summary>
        public readonly int Duration;

        public readonly StopInfo<T> Departure, Arrival;

        public readonly ViasInfo<T> Vias;

        public JourneyInfo(int id, int duration, StopInfo<T> departure, StopInfo<T> arrival, List<ViaElement<T>> vias)
        {
            Id = id;
            Duration = duration;
            Departure = departure;
            Arrival = arrival;
            Vias = new ViasInfo<T>(vias);
        }
        
        

        /// <summary>
        /// Construct a new JourneyInfo.
        /// Due to the generic type argument, this could not be a constructor.
        /// </summary>
        /// <returns></returns>
        public static JourneyInfo<T> FromJourney<T>(PublicTransportRouter router, int id, Journey<T> journey)
            where T : IJourneyStats<T>
        {
            var connections = journey.AllParts();

            var duration = (int)
                (connections.Last().Time - connections[0].Time);

            // We take the second element in the list, as the  first element is but a 'genesisConnection'
            var departure = new StopInfo<T>(router, connections[0]);
            var arrival = new StopInfo<T>(router, connections.Last());

            var viaEls = new List<ViaElement<T>>();

            //Search for transfers
            for (var i = 1; i < connections.Count - 1; i++)
            {
                var conn = connections[i];

                if (!(conn.SpecialConnection && conn.Connection == Journey<T>.TRANSFER)) continue;


                var viaEl =
                    new ViaElement<T>(router, viaEls.Count,
                        connections[i - 1],connections[i], connections[i + 1]);

                viaEls.Add(viaEl);
            }

            return new JourneyInfo<T>(id, duration, departure, arrival, viaEls);
        }


        public class ViasInfo<T0> where T0 : IJourneyStats<T0>
        {
            public readonly int Number;
            public readonly List<ViaElement<T0>> Via;

            public ViasInfo(List<ViaElement<T0>> via)
            {
                Number = via.Count;
                Via = via;
            }
        }
    }
}