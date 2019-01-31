using System;
using Itinero.Transit.Data;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Is responsible of sweeping over the transitDB in order to refresh all the connections
    /// </summary>
    public class ConnectionRefresher
    {

        private readonly TransitDb _db;

        public ConnectionRefresher(TransitDb db)
        {
            _db = db;
        }

        public void Refresh(DateTime start, DateTime end)
        {
            var windows =  _db.LoadedTimeWindows;
            foreach (var (wStart, wEnd) in windows)
            {
                if (wStart < start || wEnd > end)
                {
                    continue;
                }
                
                _db.UpdateTimeFrame(wStart, wEnd);
                
            }
        }
        
    }
}