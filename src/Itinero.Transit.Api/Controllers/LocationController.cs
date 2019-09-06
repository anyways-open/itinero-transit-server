using System;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

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
        [HttpGet]
        public ActionResult<Location> Get(string id)
        {
            var found = State.GlobalState.LocationOf(id);
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
        [HttpGet("connections")]
        public ActionResult<LocationSegmentsResult> GetConnections(string id, DateTime windowStart,
            int windowLength = 3600)
        {
            if (windowStart == default)
            {
                windowStart = DateTime.Now;
            }

            windowStart = windowStart.ToUniversalTime();

            var windowEnd = windowStart.AddSeconds(windowLength);

            var found = State.GlobalState.SegmentsForLocation(id, windowStart, windowEnd);
            if (found == null)
            {
                return NotFound("No location with this id found");
            }

            return found;
        }
    }
}