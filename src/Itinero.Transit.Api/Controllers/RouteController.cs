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
            var _profile = State.GlobalState.OtherModeBuilder.GetOsmProfile(profileName);

            var _routerDb = State.GlobalState.RouterDb;

            var startPoint = _routerDb.Snap((float) fromLon, (float) fromLat);
            var endPoint = _routerDb.Snap((float) toLon, (float) toLat);

            if (startPoint.IsError || endPoint.IsError)
            {
                return BadRequest("Could not snap to nearest roads");
            }

            var route = _routerDb.Calculate(_profile, startPoint.Value, endPoint.Value);

            if (route.IsError)
            {
                return BadRequest("Didn't find a route");
            }

            return route.Value.Shape.Select(coor => new Coordinate(coor.Latitude, coor.Longitude)).ToList();
        }
    }
}