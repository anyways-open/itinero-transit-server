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
        /// Searches for stops which are at most 500 meters north, east, south or west from the specified point.
        /// </summary>
        /// <remarks>
        /// Note that we construct a bounding square around `(lat, lon)`, namely `(lat - distance, lon - distance, lat + distance, lon + distance)`.
        /// This implies that the maximal (Euclidian) distance between the specified location and a resulting location can be up to `sqrt(2) * distance`.
        /// </remarks>
        /// <param name="lat">The WGS84-latitude point of where to search</param>
        /// <param name="lon">The WGS84-longitude point of where to search</param>
        /// <param name="distance">The maximal distance north, east, south or west that the resulting locations could be.</param>
        [HttpGet]
        public ActionResult<Locations> Get(float lat, float lon, float distance=500)
        {
            var found = StopsDb.LocationsInRange(lat, lon, distance);
            if (found == null || !found.Any())
            {
                return NotFound($"Could not find any stop close by ({lat} lat,{lon} lon) within {distance}m");
            }
            
            return new Locations(StopsDb.GetReader(), found);
        }
    }
}