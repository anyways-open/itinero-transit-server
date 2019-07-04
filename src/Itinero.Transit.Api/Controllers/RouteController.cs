using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
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
            string profileName
        )
        {


            return null;
        }
    }
}