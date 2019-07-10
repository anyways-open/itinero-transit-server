using System;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Logic.Itinero.Transit.Journey.Metric;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class JourneyController : ControllerBase
    {
        public const string DefaultWalkGenerator =
            "crowsflight&maxDistance=500&speed=1.4";

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
        /// <param name="inBetweenOsmProfile">Overrides the walksGeneratorDescription with an OSM-profile for walks between PT-stops</param>
        /// <param name="inBetweenSearchDistance">The search distance for walks, only if inBetweenOsmProfile is specified</param>
        /// <param name="firstMileOsmProfile">Use this OSM-profile for the first segment</param>
        /// <param name="firstMileSearchDistance">Search distance for the first segment, if first mile is specified</param>
        /// <param name="lastMileOsmProfile">Use this OSM-profile for the last segment</param>
        /// <param name="lastMileSearchDistance">Search distance for the last segment mile, if first mile is specified</param>
        /// <param name="multipleOptions">Set to true if multiple options (profile search) should be used</param>
        [HttpGet]
        public ActionResult<QueryResult> Get(
            string from,
            string to,
            string walksGeneratorDescription = DefaultWalkGenerator,
            string inBetweenOsmProfile = null,
            uint inBetweenSearchDistance = 1500,
            string firstMileOsmProfile = null,
            uint firstMileSearchDistance = 1500,
            string lastMileOsmProfile = null,
            uint lastMileSearchDistance = 1500,
            DateTime? departure = null,
            DateTime? arrival = null,
            uint internalTransferTime = 180,
            uint maxNumberOfTransfers = 4,
            bool prune = true,
            bool multipleOptions = false
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

            try
            {
                if (inBetweenOsmProfile != null)
                {
                    walksGeneratorDescription =
                        $"osm&maxDistance={inBetweenSearchDistance}&profile={inBetweenOsmProfile}";
                }

                if (firstMileOsmProfile != null)
                {
                    firstMileOsmProfile =
                        $"osm&maxDistance={firstMileSearchDistance}&profile={firstMileOsmProfile}";
                }

                if (lastMileOsmProfile != null)
                {
                    lastMileOsmProfile =
                        $"osm&maxDistance={lastMileSearchDistance}&profile={lastMileOsmProfile}";
                }

                if (firstMileOsmProfile != null || lastMileOsmProfile != null)
                {
                    firstMileOsmProfile = firstMileOsmProfile ?? walksGeneratorDescription;
                    lastMileOsmProfile = lastMileOsmProfile ?? walksGeneratorDescription;
                    walksGeneratorDescription =
                        "firstLastMile" +
                        "&firstMile=" + Uri.EscapeDataString(firstMileOsmProfile) +
                        "&default=" + Uri.EscapeDataString(walksGeneratorDescription) +
                        "&lastMile=" + Uri.EscapeDataString(lastMileOsmProfile);
                }


                var profile = JourneyBuilder.CreateProfile(from, to,
                    walksGeneratorDescription,
                    internalTransferTime,
                    maxNumberOfTransfers: maxNumberOfTransfers);

                var (journeys, queryStart, queryEnd) =
                    profile.BuildJourneys(from, to, departure, arrival, multipleOptions);


                // ReSharper disable once PossibleMultipleEnumeration
                if (journeys == null || !journeys.Any())
                {
                    return new QueryResult(null, start, DateTime.Now, queryStart, queryEnd);
                }


                if (prune)
                {
                    journeys = journeys.PruneInAlternatives
                    (TravellingTimeMinimizer.Factory,
                        new TravellingTimeMinimizer.Minimizer(State.GlobalState.ImportancesInternal));
                }

                var end = DateTime.Now;

                // ReSharper disable once PossibleMultipleEnumeration
                return new QueryResult(State.GlobalState.Translate(journeys, profile.WalksGenerator),
                    start, end,
                    queryStart, queryEnd);
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}