using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;
using Itinero.Transit.IO.OSM;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class OsmRouteController : ControllerBase
    {
        /// <summary>
        /// Allows to test route-planning over the OSM network with the specified profile.
        /// </summary>
        /// <param name="fromLat">E.g. 51.1989</param>
        /// <param name="fromLon">E.g. 3.2255</param>
        /// <param name="toLat">E.g. 51.2190</param>
        /// <param name="toLon">E.g; 3.2278</param>
        /// <param name="profileName">A profile, e.g. pedestrian, bicycle. See /status to see what is loaded</param>
        /// <param name="maxSearch">The maximum walking distance in meters, e.g. 2500</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<Geojson> Get(
            float fromLat,
            float fromLon,
            float toLat,
            float toLon,
            string profileName = "pedestrian",
            uint maxSearch = 2500
        )
        {
            try
            {
                var coordinates = RouteBuilder.Get(fromLat, fromLon, toLat, toLon, profileName, maxSearch);
                if (coordinates == null || !coordinates.Any())
                {
                    return BadRequest(
                        "Could not calculate a route. Check that departure and arrival points are close to a road, and that the maxSearch is not too low");
                }

                var geometry = new Geometry(coordinates);

                return new Geojson(new List<Feature> {new Feature(geometry, new Properties("#000000"))});
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
    }
}