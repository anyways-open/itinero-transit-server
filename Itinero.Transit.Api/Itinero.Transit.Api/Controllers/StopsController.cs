using System.Net;
using Itinero.Transit.Data;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Stop = Itinero.Transit.Api.Models.Stop;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]   
    [ProducesResponseType(404)]  
    public class StopsController : ControllerBase
    {
        public static StopsDb StopsDb;

        
        /// <summary>
        /// Gets information about a location, based on the location id
        /// </summary>
        /// <param name="id">The identifier of the location, e.g. 'https://irail.be/stations/NMBS/008891009'</param>
        [HttpGet]
        public ActionResult<Stop> Get(string id)
        {
            var reader = StopsDb.GetReader();
            if (!reader.MoveTo(id))
            {
                return NotFound("No location with this id found");
            }
            
            return new Stop(reader);
        }
    }
}