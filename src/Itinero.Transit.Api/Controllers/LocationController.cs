using System;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]   
    [ProducesResponseType(404)]  
    public class LocationController : ControllerBase
    {
        /// <summary>
        /// Gets information about a location, based on the location id
        /// </summary>
        /// <param name="id">The identifier of the location, e.g. 'http://irail.be/stations/NMBS/008891009'</param>
        [HttpGet]
        public ActionResult<Location> Get(string id)
        {
            var found = State.TransitDb.Latest.LocationOf(id);
            if (found == null)
            {
                return NotFound("No location with this id found");
            }
            return  found;
        }
        
        /// <summary>
        /// Gets information about a location, based on the location id
        /// </summary>
        /// <param name="id">The identifier of the location, e.g. 'http://irail.be/stations/NMBS/008891009'</param>
        [HttpGet("connections")]
        public ActionResult<LocationSegmentsResult> GetConnections(string id)
        {
            var found = State.TransitDb.Latest.SegmentsForLocation(id, DateTime.Now, TimeSpan.FromHours(1));
            if (found == null)
            {
                return NotFound("No location with this id found");
            }
            return  found;
        }
    }
}