using System;
using System.Collections.Generic;
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
            var loadedTimeWindows = new Dictionary<string, IEnumerable<(DateTime start, DateTime end)>>();
            var tasks = new Dictionary<string, string>();

            var state = State.GlobalState;
            if (state.TransitDbs != null)
            {
                foreach (var (name, (_, synchronizer)) in state.TransitDbs)
                {
                    loadedTimeWindows.Add(name, synchronizer.LoadedTimeWindows);
                    if (synchronizer.CurrentlyRunning != null)
                    {
                        tasks.Add(name, synchronizer.CurrentlyRunning.ToString());
                    }
                }
            }

            tasks.Add("freemessage", state.FreeMessage);


            var report = new StatusReport(
                state.BootTime,
                (long) (DateTime.Now - state.BootTime).TotalSeconds,
                loadedTimeWindows,
                State.Version,
                tasks
            );
            return report;
        }
    }
}