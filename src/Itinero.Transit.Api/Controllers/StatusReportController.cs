using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Logic.Transfers;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Utils;
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

            var tasks = new Dictionary<string, string>();
            var reports = new Dictionary<string, OperatorStatus>();
            foreach (var provider in state.Operators.All)
            {
                var connsDb = provider.Tdb.Latest.ConnectionsDb;
                TimeWindow window = null;
                if (connsDb != null && connsDb.EarliestDate != ulong.MaxValue && connsDb.LatestDate != ulong.MinValue)
                {
                    window = new TimeWindow(connsDb.EarliestDate.FromUnixTime(), connsDb.LatestDate.FromUnixTime());
                }

                var report = new OperatorStatus(provider.AltNames.ToList(), provider.Tags.ToList(), window);
                reports.Add(provider.Name, report);
            }


            tasks.Add("statusmessage", state.FreeMessage);


            return new StatusReport(
                state.BootTime,
                (long) (DateTime.Now - state.BootTime).TotalSeconds,
                reports,
                State.Version,
                tasks,
                state.OtherModeBuilder.SupportedUrls(),
                state.OtherModeBuilder.OsmVehicleProfiles.Select(prof => prof.Name).ToList(), _loadedTilesCount
            );
        }
    }
}