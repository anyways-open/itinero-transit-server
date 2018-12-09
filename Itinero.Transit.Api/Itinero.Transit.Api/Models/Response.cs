using System;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Journeys;
using Reminiscence.Collections;
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api
{
    
    /// <summary>
    /// Returns multiple journeys as JSON
    /// </summary>
    internal class IrailResponse<T> where T : IJourneyStats<T>
    {

        public readonly string Version = "1.1";

        public readonly int Timestamp;

        // Yes; this name screams to be be connectionS. Sadly, it is not named like that in IRail-jsons
        public readonly List<JourneyInfo<T>> Connection;

        public IrailResponse(List<JourneyInfo<T>> journeys)
        {
            Timestamp = (int) (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            Connection = journeys;
        }

        public static IrailResponse<T> CreateResponse(PublicTransportRouter router, System.Collections.Generic.IEnumerable<Journey<T>> journeys)
        {
            var journeyInfo = new List<JourneyInfo<T>>();
            foreach (var journey in journeys)
            {
                journeyInfo.Add(JourneyInfo<T>.FromJourney(router, journeyInfo.Count, journey));
            }
            return new IrailResponse<T>(journeyInfo);
        }

        public static IrailResponse<T> CreateResponse(PublicTransportRouter router, Journey<T> journey)
        {
            return CreateResponse(router, new List<Journey<T>> {journey});
        }
    }
}