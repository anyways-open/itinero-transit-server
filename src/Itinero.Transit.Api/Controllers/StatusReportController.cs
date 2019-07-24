using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.IO.OSM;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class StatusController : ControllerBase
    {



        private uint _loadedTilesCount;
        private DateTime _lastTileIndexation = DateTime.MinValue;
        
        /// <summary>
        /// Gives some insight in the database
        /// </summary>
        [HttpGet]
        public ActionResult<StatusReport> Get()
        {
            var loadedTimeWindows = new Dictionary<string, IEnumerable<(DateTime start, DateTime end)>>();
            var tasks = new Dictionary<string, string>();


            if ((DateTime.Now - _lastTileIndexation).TotalMinutes > 2)
            {
                _loadedTilesCount = OsmTransferGenerator.LoadedTilesCount();
                _lastTileIndexation = DateTime.Now;
            }
            var state = State.GlobalState;
            if (state == null)
            {
                
                
                return new StatusReport(
                    DateTime.Now,
                    0,
                    null,
                    State.Version,
                    new Dictionary<string, string>()
                        {{"statusmessage", "Still booting, hang on"}},
                    new List<string>(), 
                    new List<string>(),
                    _loadedTilesCount
                );
            }

            if (state.TransitDbs != null)
            {
                foreach (var (name, (_, synchronizer)) in state.TransitDbs)
                {
                    if (synchronizer.LoadedTimeWindows != null)
                    {
                        loadedTimeWindows.Add(name, synchronizer.LoadedTimeWindows);
                    }

                    if (synchronizer.CurrentlyRunning != null)
                    {
                        tasks.Add(name, synchronizer.CurrentlyRunning.ToString());
                    }
                }
            }

            
            
            tasks.Add("statusmessage", state.FreeMessage);


            return new StatusReport(
                state.BootTime,
                (long) (DateTime.Now - state.BootTime).TotalSeconds,
                loadedTimeWindows,
                State.Version,
                tasks,
                state.OtherModeBuilder.SupportedUrls(),
                state.OtherModeBuilder.OsmVehicleProfiles.Select(prof => prof.Name).ToList(), _loadedTilesCount
            );
        }
    }
}