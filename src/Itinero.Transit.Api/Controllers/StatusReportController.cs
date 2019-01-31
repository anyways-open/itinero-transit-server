using System;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class StatusController : ControllerBase
    {
        /// <summary>
        /// Gives some insight in the database
        /// </summary>
        [HttpGet]
        public ActionResult<StatusReport> Get()
        {
            var report = new StatusReport(
                State.BootTime,
                (long) (DateTime.Now - State.BootTime).TotalSeconds,
                State.TransitDb.LoadedTimeWindows,
                State.Version
            );
            return report;
        }
    }
}