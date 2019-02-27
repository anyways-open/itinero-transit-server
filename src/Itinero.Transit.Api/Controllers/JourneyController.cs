using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.Journeys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public class JourneyController : ControllerBase
    {
        /// <summary>
        /// Creates a journey over the public-transport network.
        /// </summary>
        /// <remarks>
        /// You do not have to provide both departure and arrival times, one of them is enough.
        /// When both are given, all possible journeys between those points in time are calculated.
        /// If only one is given, a fastest possible test route is created. Then, the taken time is doubled and all possible journeys between `(journey.Departure, journey.Departure + 2*journey.TotalTime)` are given.
        /// </remarks>
        /// <param name="from">The location where the journey starts, e.g. http://irail.be/stations/NMBS/008891009</param>
        /// <param name="to">The location where the traveller would like to go, e.g. http://irail.be/stations/NMBS/008892007</param>
        /// <param name="departure">The earliest moment when the traveller would like to depart, in ISO8601 format</param>
        /// <param name="arrival">The last moment where the traveller would like to arrive, in ISO8601 format</param>
        /// <param name="internalTransferTime">The number of seconds the traveller needs to transfer trains within the station. Increase for less mobile users</param>
        /// <param name="prune">If false, more options will be given (mostly choices in transfer station)</param>
        [HttpGet]
        public ActionResult<List<Journey>> Get(
            string from,
            string to,
            DateTime? departure = null,
            DateTime? arrival = null,
            uint internalTransferTime = 180,
            bool prune = true
        )
        {
            if (Equals(from, to))
            {
                return BadRequest("The given from- and to- locations are the same");
            }

            var p = new Profile<TransferStats>(
                new InternalTransferGenerator(internalTransferTime),
                null,
                TransferStats.Factory,
                TransferStats.ProfileTransferCompare);
            var journeys = State.TransitDb.Latest.CalculateJourneys(
                p, from, to, departure, arrival
            );

            // ReSharper disable once PossibleMultiplepEnumeration
            if (journeys == null || !journeys.Any())
            {
                return NotFound("No possible journeys were found for your request");
            }

            if (prune)
            {
                journeys = journeys.PruneInAlternatives
                (TravellingTimeMinimizer.Factory,
                    new TravellingTimeMinimizer.Minimizer(State.ImportancesInternal));
            }

            // ReSharper disable once PossibleMultipleEnumeration
            return State.TransitDb.Latest.Translate(journeys);
        }
    }
}