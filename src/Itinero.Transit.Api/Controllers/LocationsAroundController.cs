using System.Linq;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]   
    [ProducesResponseType(404)]  
    public class LocationsAroundController : ControllerBase
    {
        public static StopsDb StopsDb;
        
        /// <summary>
        /// Searches for stops which are at most 'within' meters north, east, south or west from the specified point.
        /// </summary>
        /// <remarks>
        /// Note that we construct a bounding box of `(lat - within, lon - within, lat + within, lon + within)`.
        /// This implies that the maximal (Euclidian) distance between the specified location and a resulting location can be
        /// sqrt(2) * within
        /// </remarks>
        /// <param name="lat">The WGS84-latitude point of where to search</param>
        /// <param name="lon">The WGS84-longitude point of where to search</param>
        /// <param name="within">The maximal distance north, east, south or west that the resulting locations could be.</param>
        [HttpGet]
        public ActionResult<Locations> Get(float lat, float lon, float within=500)
        {
            var found = StopsDb.LocationsInRange(lat, lon, within);
            if (found == null || !found.Any())
            {
                return NotFound($"Could not find any stop close by ({lat} lat,{lon} lon) within {within}m");
            }
            
            return new Locations(StopsDb.GetReader(), found);
        }
    }
}