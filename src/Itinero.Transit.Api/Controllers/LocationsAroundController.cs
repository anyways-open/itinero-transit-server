using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Algorithms.Search;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable PossibleMultipleEnumeration

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]   
    [ProducesResponseType(404)]  
    public class LocationsAroundController : ControllerBase
    {
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
        public ActionResult<List<Location>> Get(float lat, float lon, float distance=500)
        {

            var stopsdb = State.TransitDb.Latest.StopsDb;
            
            var reader = stopsdb.GetReader();
            
            var found = reader.LocationsInRange(lat, lon, distance);
            if (found == null || !found.Any())
            {
                return NotFound($"Could not find any stop close by ({lat} lat,{lon} lon) within {distance}m");
            }

            var locations = new List<Location>();
            foreach (var location in found)
            {
                // The 'found' list does _not_ contain the attributes such as 'Name'
                // So we query each station individually again
                reader.MoveTo(location.Id);
                locations.Add(new Location(reader));
            }
            
            return locations;
        }
    }
}