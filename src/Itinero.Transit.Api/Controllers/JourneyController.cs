using System;using System.Collections.Generic;using System.Linq;using Itinero.Transit.Api.Logic;using Itinero.Transit.Api.Logic.Importance;using Itinero.Transit.Api.Models;using Itinero.Transit.Journey.Metric;using Microsoft.AspNetCore.Mvc;using Serilog;namespace Itinero.Transit.Api.Controllers{    [Route("[controller]")]    [ApiController]    [ProducesResponseType(200)]    public class JourneyController : ControllerBase    {        public const string DefaultWalkGenerator =            "crowsflight&maxDistance=500&speed=1.4";        /// <summary>        /// Calculates multiple journeys over the public-transport network.        /// </summary>        /// <remarks>        ///         /// PRIVACY NOTICE: on routing.anyways.eu, all requests are logged.        /// We keep track of all the parameters entered, in order to improve performance of the service.        /// We do _not_ track IP address or other data points which can tie a request to a user (except for location itself).        /// This data is not given to third parties.        /// However, aggregate data (e.g. a map showing where popular destinations or departure points are) could be published in the future.)        /// /PRIVACY NOTICE        /// <br />        /// <br />        ///         ///         /// You do not have to provide both departure and arrival times, one of them is enough.        /// When both are given, all possible journeys between those points in time are calculated.        /// If only one is given, a fastest possible test route is created. Then, the taken time is doubled and all possible journeys between `(journey.Departure, journey.Departure + 2*journey.TotalTime)` are given.        /// </remarks>        /// <param name="from">The location where the journey starts, e.g. http://irail.be/stations/NMBS/008891009 . Alternatively, an OSM-url is supported as well, such as https://www.openstreetmap.org/#map=19/51.21576/3.22001</param>        /// <param name="to">The location where the traveller would like to go, e.g. http://irail.be/stations/NMBS/008892007</param>        /// <param name="departure">The earliest moment when the traveller would like to depart, in ISO8601 format (e.g. `2019-12-31T23:59:59Z` where Z is the timezone)</param>        /// <param name="arrival">The last moment where the traveller would like to arrive, in ISO8601 format</param>        /// <param name="operators">The name(s) of the operators(s) OR the tag(s) to perform route planning on. If a tag is specified, all the operators with this tag will be used. Names and tags can be mixed. All are separated by ';'. Use '*' to match all operators</param>        /// <param name="internalTransferTime">The number of seconds the traveller needs to transfer trains within the station. Increase for less mobile users</param>        /// <param name="walksGeneratorDescription">Describes the walk policy, such as a crows flight policy or osm-routing policy. These are URLs as well, have a look at /status to see what walk policies are supported. To have a different first mile and last mile, use the 'FirstLastMile' policy which exhibits this complex behaviour</param>        /// <param name="maxNumberOfTransfers">The maximum number of transfers allowed during the earliest arrival scan</param>        /// <param name="prune">If false, more options will be given (mostly choices in transfer station)</param>        /// <param name="inBetweenOsmProfile">Overrides the walksGeneratorDescription with an OSM-profile for walks between PT-stops</param>        /// <param name="inBetweenSearchDistance">The search distance for walks, only if inBetweenOsmProfile is specified</param>        /// <param name="firstMileOsmProfile">Use this OSM-profile for the first segment</param>        /// <param name="firstMileSearchDistance">Search distance for the first segment, if first mile is specified</param>        /// <param name="lastMileOsmProfile">Use this OSM-profile for the last segment</param>        /// <param name="lastMileSearchDistance">Search distance for the last segment mile, if first mile is specified</param>        /// <param name="multipleOptions">Set to true if multiple options (profile search) should be used</param>        /// <param name="compress">If true, walks will be given in a separate section of the json and will be referred to by an index. This is to compress the result</param>        /// <param name="test">Does nothing - please leave false. We run hourly tests against the service to be notified of failures. The test queries have this flag enabled, in order to save them into a different log file</param>        [HttpGet]        public ActionResult<QueryResult> Get(            string from,            string to,            string walksGeneratorDescription = DefaultWalkGenerator,            string inBetweenOsmProfile = null,            uint inBetweenSearchDistance = 0,            string firstMileOsmProfile = null,            uint firstMileSearchDistance = 1500,            string lastMileOsmProfile = null,            uint lastMileSearchDistance = 1500,            DateTime? departure = null,            DateTime? arrival = null,            string operators = "*",            uint internalTransferTime = 180,            uint maxNumberOfTransfers = 4,            bool prune = true,            bool multipleOptions = false,            bool compress = false,            bool test = false        )        {            if (State.GlobalState == null)            {                return BadRequest(                    "The server is still booting and not able to handle your request at this time. Please come back later. Be aware that queries are already answered even if the data is still loading.");            }            if (from == null || to == null)            {                return BadRequest("From or To is missing");            }            if (operators == null)            {                operators = "*";            }            var start = DateTime.Now;            var logMessage = new Dictionary<string, string>            {                {"departure", $"{departure:s}"},                {"arrival", $"{arrival:s}"},                {"internalTransferTime", "" + internalTransferTime},                {"prune", "" + prune},                {"maxNumberOfTransfers", "" + maxNumberOfTransfers},                {"multipleOptions", "" + multipleOptions},                {"operators", operators}            };            try            {                from = Uri.UnescapeDataString(from);                to = Uri.UnescapeDataString(to);                if (Equals(from, to))                {                    return BadRequest("The given from- and to- locations are the same");                }                logMessage.Add("from", from);                logMessage.Add("to", to);                walksGeneratorDescription = CreateWalksGeneratorDescription(walksGeneratorDescription,                    inBetweenOsmProfile, inBetweenSearchDistance, firstMileOsmProfile, firstMileSearchDistance,                    lastMileOsmProfile, lastMileSearchDistance);                logMessage.Add("walksGenerator", walksGeneratorDescription);                var operatorSet = State.GlobalState.Operators.GetView(operators);                var profile = operatorSet.CreateProfile(from, to,                    walksGeneratorDescription,                    internalTransferTime,                    maxNumberOfTransfers: maxNumberOfTransfers);                if (profile.WalksGenerator.Range() > 100000)                {                    return BadRequest("Walking range to big, should at most be 100km");                }                var (journeys, directWalk, queryStart, queryEnd) =                    profile.BuildJourneys(from, to, departure, arrival, multipleOptions, logMessage);                logMessage.Add("foundJourneys", (journeys?.Count ?? 0) + "");                logMessage.Add("directWalkFound", (directWalk != null) + "");                if (journeys != null && journeys.Any() &&                    prune && State.GlobalState.ImportancesInternal != null)                {                    journeys = journeys.PruneFamilies(                        new ImportanceMaximizer<TransferMetric>(State.GlobalState.ImportancesInternal, operatorSet.GetStops()));                }                LogRequest(test, start, logMessage);                var (translatedJourneys, commonCoordinates) =                    operatorSet.Translate(journeys, profile.WalksGenerator, compress);                return new QueryResult(                    translatedJourneys,                    start, DateTime.Now,                    queryStart, queryEnd, profile.WalksGenerator.OtherModeIdentifier(),                    directWalk,                    commonCoordinates);            }            catch (ArgumentException e)            {                Log.Warning("Someone used wrong arguments: " + e.Message + "\nParameters are: " +                            $"from={from}\n&to={to}\n&walksGeneratorDescription={walksGeneratorDescription}\n&" +                            $"departure={departure:s}&\narrival={arrival:s}&" +                            $"\nprune={prune}&multipleOptions={multipleOptions}");                var extraMessage = "";#if DEBUG                extraMessage = e.Message + "\n" + e.StackTrace;#endif                logMessage.Add("errormessage", e.Message + extraMessage);                LogRequest(test, start, logMessage);                Log.Warning(e.ToString());                return BadRequest("There was something wrong with the parameters:" + e.Message + extraMessage);            }            catch (Exception e)            {                Log.Error(e, $"Unhandled exception in {nameof(JourneyController)}.{nameof(Get)}. Parameters are:" +                             $"from={from}\n&to={to}\n&walksGeneratorDescription={walksGeneratorDescription}\n&" +                             $"departure={departure:s}&\narrival={arrival:s}&" +                             $"\nprune={prune}&multipleOptions={multipleOptions}");                logMessage.Add("errormessage", e.Message);                LogRequest(test, start, logMessage);                var extraMessage = "";#if DEBUG                extraMessage = e.Message + "\n" + e.StackTrace;#endif                return BadRequest("Internal server error\n" + extraMessage);            }        }        private static string CreateWalksGeneratorDescription(            string walksGeneratorDescription,            string inBetweenOsmProfile,            uint inBetweenSearchDistance,            string firstMileOsmProfile,            uint firstMileSearchDistance,            string lastMileOsmProfile,            uint lastMileSearchDistance)        {            if (inBetweenOsmProfile != null)            {                if (inBetweenOsmProfile.ToLower() == "crowsflight")                {                    walksGeneratorDescription = $"crowsflight&maxDistance={inBetweenSearchDistance}";                }                else                {                    walksGeneratorDescription =                        $"osm&maxDistance={inBetweenSearchDistance}&profile={inBetweenOsmProfile}";                }            }            if (firstMileOsmProfile != null)            {                firstMileOsmProfile =                    $"osm&maxDistance={firstMileSearchDistance}&profile={firstMileOsmProfile}";            }            if (lastMileOsmProfile != null)            {                lastMileOsmProfile =                    $"osm&maxDistance={lastMileSearchDistance}&profile={lastMileOsmProfile}";            }            if (firstMileOsmProfile != null || lastMileOsmProfile != null)            {                firstMileOsmProfile = firstMileOsmProfile ?? walksGeneratorDescription;                lastMileOsmProfile = lastMileOsmProfile ?? walksGeneratorDescription;                walksGeneratorDescription =                    "firstLastMile" +                    "&firstMile=" + Uri.EscapeDataString(firstMileOsmProfile) +                    "&default=" + Uri.EscapeDataString(walksGeneratorDescription) +                    "&lastMile=" + Uri.EscapeDataString(lastMileOsmProfile);            }            return walksGeneratorDescription;        }        private void LogRequest(bool isTest, DateTime startMoment, Dictionary<string, string> logMessage)        {            var end = DateTime.Now;            var timeNeeded = (int) (end - startMoment).TotalMilliseconds;            logMessage["timeneeded"] = "" + timeNeeded;            State.GlobalState.Logger?.WriteLogEntry(isTest ? "testrequest" : "request", logMessage);        }    }}