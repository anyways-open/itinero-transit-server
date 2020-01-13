using System.Collections.Generic;
using System.Linq;
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
    public class LocationsAroundController : ControllerBase
    {
        /// <summary>
        /// Searches for stops which are at most 500 meters away from the specified point.
        /// </summary>
        /// <remarks>
        /// Note that we construct a bounding square around `(lat, lon)`, namely `(lat - distance, lon - distance, lat + distance, lon + distance)`.
        /// This implies that the maximal (Euclidian) distance between the specified location and a resulting location can be up to `sqrt(2) * distance`.
        /// </remarks>
        /// <param name="lat">The WGS84-latitude point of where to search</param>
        /// <param name="lon">The WGS84-longitude point of where to search</param>
        /// <param name="operators">The name(s) of the operators(s) OR the tag(s) to perform route planning on. If a tag is specified, all the operators with this tag will be used. Names and tags can be mixed. All are separated by ';'. Use '*' to match all operators</param>
        /// <param name="distance">The maximal distance north, east, south or west that the resulting locations could be.</param>
        [HttpGet]
        public ActionResult<List<Location>> Get(float lat, float lon, string operators = "*", uint distance = 500)
        {
            var stopsDb = State.GlobalState.Operators
                .GetView(operators)
                .GetStops();

            return stopsDb.GetInRange((lon, lat), distance)
                .Select(stop => new Location(stop)).ToList();
        }
    }
}