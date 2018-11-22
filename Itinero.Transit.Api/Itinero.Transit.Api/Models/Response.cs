using System;
using Itinero.Transit.Api.Logic;
using Reminiscence.Collections;
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api
{
    
    /// <summary>
    /// Returns multiple journeys as JSON
    /// </summary>
    public class IrailResponse
    {

        public readonly string Version = "1.1";

        public readonly int Timestamp;

        // Yes; this name screams to be be connectionS. Sadly, it is not named like that in IRail-jsons
        public readonly List<JourneyInfo> Connection;

        public IrailResponse(List<JourneyInfo> journeys)
        {
            Timestamp = (int) (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            Connection = journeys;
        }

        public IrailResponse(JourneyInfo journey) : this(new List<JourneyInfo> {journey})
        {
            
        }


        public static IrailResponse CreateResponse<T>(PublicTransportRouter router, System.Collections.Generic.IEnumerable<Journey<T>> journeys)
            where T : IJourneyStats<T>
        {
            var journeyInfo = new List<JourneyInfo>();
            foreach (var journey in journeys)
            {
                journeyInfo.Add(JourneyInfo.FromJourney(router, journeyInfo.Count, journey));
            }
            return new IrailResponse(journeyInfo);
        }

        public static IrailResponse CreateResponse<T>(PublicTransportRouter router, Journey<T> journey) 
            where T : IJourneyStats<T>
        {
            return CreateResponse(router, new List<Journey<T>> {journey});
        }


    }
}