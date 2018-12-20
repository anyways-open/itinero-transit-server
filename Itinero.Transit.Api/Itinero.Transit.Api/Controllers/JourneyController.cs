using System;
using System.Linq;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public class JourneyController : ControllerBase
    {
        public static JourneyTranslator Translator;


        /// <summary>
        /// Creates a journey over the public-transport network
        /// </summary>
        /// <param name="from">The location where the journey starts, e.g. https://irail.be/stations/NMBS/008891009</param>
        /// <param name="to">The location where the traveller would like to go, e.g. https://irail.be/stations/NMBS/008892007</param>
        /// <param name="departure">The earliest moment when the traveller would like to depart, in ISO8601 format</param>
        /// <param name="arrival">The last moment where the traveller would like to arrive, in ISO8601 format</param>
        /// <param name="internalTransferTime">The number of seconds the traveller needs to transfer trains within the station. Increase for less mobile users</param>
        [HttpGet]
        public ActionResult<Models.Journeys> Get(
            string from,
            string to,
            DateTime departure,
            DateTime arrival,
            uint internalTransferTime = 180
        )
        {

            if (Equals(from, to))
            {
                return BadRequest("The given from- and to- locations are the same");
            }

            try
            {

                Translator.InternalidOf(from);
                Translator.InternalidOf(to);
            }
            catch
            {
                return BadRequest("A station could not be found. Check the identifiers");
            }
            
            var p = new Profile<TransferStats>(
                Translator.Db.Connections,
                Translator.Db.Stops,
                new InternalTransferGenerator(internalTransferTime),
                null,
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var journeys = p.CalculateJourneys(
                from, to, departure, arrival
            );

            if (journeys == null || !journeys.Any())
            {
                return NotFound("No possible journeys were found for your request");
            }

            return Translator.Translate(journeys);
        }
    }
}