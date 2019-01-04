using System;
using Itinero.Transit.Api.Models;

namespace Itinero.Transit.Api.Logic
{
    public class StatusReportGenerator
    {
        private readonly DateTime _bootTime;
        private readonly DatabaseLoader _db;

        public StatusReportGenerator(DatabaseLoader db)
        {
            _db = db;
            _bootTime = DateTime.Now;
        }

        public StatusReport CreateReport()
        {
           /* int loadedLocations, int loadedConnections,
                DateTime firstConnectionDeparture, DateTime lastConnectionDeparture, 
                DateTime targetLastConnectionDeparture
            */
            var report = new StatusReport(
                _bootTime,
                (long) (DateTime.Now - _bootTime).TotalSeconds,
                _db.Status.LoadedLocationsCount,
                _db.Status.TargetLocationsCount,
                _db.Status.LoadedConnectionsCount,
                _db.Status.FirstDepartureTime,
                _db.Status.LastDepartureTime,
                _db.Status.TargetLastDeparture
                
            );
            return report;
        }
    }
}