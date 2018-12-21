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
        public static JourneyTranslator Translator;

        
        /// <summary>
        /// Gets information about a location, based on the location id
        /// </summary>
        /// <param name="id">The identifier of the location, e.g. 'http://irail.be/stations/NMBS/008891009'</param>
        [HttpGet]
        public ActionResult<Location> Get(string id)
        {
            var found = Translator.LocationOf(id);
            if (found == null)
            {
                return NotFound("No location with this id found");
            }
            return  found;
        }
    }
}