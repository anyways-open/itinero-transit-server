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
    public class RouteController : ControllerBase
    {
        public ActionResult<List<Coordinate>> Get(
            float fromLat,
            float fromLon,
            float toLat,
            float toLon,
            string profileName = "pedestrian",
            uint maxSearch = 2500
        )
        {
            return RouteBuilder.Get(fromLat, fromLon, toLat, toLon, profileName, maxSearch);
        }
    }
}