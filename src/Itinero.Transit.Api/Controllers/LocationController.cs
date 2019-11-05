using System;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class LocationController : ControllerBase
    {
        /// <summary>
        /// Gets information about a location, based on the location id
        /// </summary>
        /// <param name="id">The identifier of the location, e.g. 'http://irail.be/stations/NMBS/008891009'</param>
        /// <param name="operators">The name(s) of the operators(s) OR the tag(s) to perform route planning on. If a tag is specified, all the operators with this tag will be used. Names and tags can be mixed. All are separated by ';'. Use '*' to match all operators</param>
        [HttpGet]
        public ActionResult<Location> Get(string id, string operators = "*")
        {
            var operatorSet = State.GlobalState?.Operators;
            if (operatorSet == null)
            {
                return BadRequest("The server is still booting. Come back later");
            }

            var found = operatorSet.GetView(operators)?.LocationOf(id);
            if (found == null)
            {
                return NotFound("No location with this id found");
            }

            return found;
        }

        /// <summary>
        /// Gives known connections at a given location.
        /// The information about the location itself is given as well. This is the same information as '/Location'
        /// </summary>
        /// <param name="id">The identifier of the location, e.g. 'http://irail.be/stations/NMBS/008891009'</param>
        /// <param name="windowStart">The start of the search window in ISO8601 format (e.g. 2019-12-31T23:59:59Z where Z is the timezone)</param>
        /// <param name="windowLength">The length of the search window in seconds. Defaults to one hour</param>
        ///      <param name="operators">The name(s) of the operators(s) OR the tag(s) to perform route planning on. If a tag is specified, all the operators with this tag will be used. Names and tags can be mixed. All are separated by ';'. Use '*' to match all operators</param>
        [HttpGet("connections")]
        public ActionResult<LocationSegmentsResult> GetConnections(string id, DateTime windowStart,
            string operators = "*",
            int windowLength = 3600)
        {
            if (State.GlobalState?.Operators == null)
            {
                return BadRequest("The server is still booting. Come back later");
            }

            if (windowStart == default)
            {
                windowStart = DateTime.Now;
            }

            windowStart = windowStart.ToUniversalTime();

            var windowEnd = windowStart.AddSeconds(windowLength);

            try
            {
                var operatorSet = State.GlobalState.Operators.GetView(operators);
                var found = operatorSet.SegmentsForLocation(id, windowStart, windowEnd);
                if (found == null)
                {
                    return NotFound("No location with this id found");
                }

                return found;
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}