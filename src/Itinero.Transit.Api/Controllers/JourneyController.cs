using System;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Logic.Itinero.Transit.Journey.Metric;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Journey.Metric;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using TravellingTimeMinimizer = Itinero.Transit.Api.Logic.Itinero.Transit.Journey.Metric.TravellingTimeMinimizer;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
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
        /// <param name="from">The location where the journey starts, e.g. http://irail.be/stations/NMBS/008891009 . Alternatively, an OSM-url is supported as well, such as https://www.openstreetmap.org/#map=19/51.21576/3.22001</param>
        /// <param name="to">The location where the traveller would like to go, e.g. http://irail.be/stations/NMBS/008892007</param>
        /// <param name="departure">The earliest moment when the traveller would like to depart, in ISO8601 format (e.g. `2019-12-31T23:59:59Z` where Z is the timezone)</param>
        /// <param name="arrival">The last moment where the traveller would like to arrive, in ISO8601 format</param>
        /// <param name="internalTransferTime">The number of seconds the traveller needs to transfer trains within the station. Increase for less mobile users</param>
        /// <param name="walksGeneratorDescription">Describes the walk policy, such as a crows flight policy or osm-routing policy. These are URLs as well, have a look at /status to see what walk policies are supported. To have a different first mile and last mile, use the 'FirstLastMile' policy which exhibits this complex behaviour</param>
        /// <param name="maxNumberOfTransfers">The maximum number of transfers allowed during the earliest arrival scan</param>
        /// <param name="prune">If false, more options will be given (mostly choices in transfer station)</param>
        [HttpGet]
        public ActionResult<QueryResult> Get(
            string from,
            string to,
            DateTime? departure = null,
            DateTime? arrival = null,
            uint internalTransferTime = 180,
            string walksGeneratorDescription =
                "https://openplanner.team/itinero-transit/walks/crowsflight&maxDistance=500",
            uint maxNumberOfTransfers = 4,
            bool prune = true
        )
        {
            if (from == null || to == null)
            {
                return BadRequest("From or To is missing");
            }
            from = Uri.UnescapeDataString(from);
            to = Uri.UnescapeDataString(to);


            if (Equals(from, to))
            {
                return BadRequest("The given from- and to- locations are the same");
            }

            var start = DateTime.Now;


            var profile = JourneyBuilder.CreateProfile(from, to,
                walksGeneratorDescription,
                internalTransferTime,
                maxNumberOfTransfers: maxNumberOfTransfers);
            var (journeys, queryStart, queryEnd) = profile.BuildJourneys(from, to, departure, arrival);

            // ReSharper disable once PossibleMultipleEnumeration
            if (journeys == null || !journeys.Any())
            {
                return BadRequest("No possible journeys were found for your request");
            }


            // ReSharper disable once InvertIf
            if (prune)
            {
                journeys = journeys.PruneInAlternatives
                (TravellingTimeMinimizer.Factory,
                    new TravellingTimeMinimizer.Minimizer(State.GlobalState.ImportancesInternal));
            }

            var end = DateTime.Now;

            // ReSharper disable once PossibleMultipleEnumeration
            return new QueryResult(State.GlobalState.Translate(journeys), start, end,
                queryStart, queryEnd);
        }
    }
}