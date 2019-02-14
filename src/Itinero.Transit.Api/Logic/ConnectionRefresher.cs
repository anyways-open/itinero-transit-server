using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Timers;
using Itinero.Transit.Data;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Is triggered every 'timespan' in order to load the required time
    /// </summary>
    public class ConnectionAutoLoader
    {
        public Func<DateTime, List<(DateTime start, DateTime end)>> GenerateWindow { get; }
        private readonly TransitDb _db;
        private readonly TimeSpan _triggerEvery;

        public ConnectionAutoLoader(TransitDb db, TimeSpan triggerEvery, TimeSpan before, TimeSpan after)
            : this(db, triggerEvery, date => new List<(DateTime start, DateTime end)> {(date - before, date + after)})
        {
        }

        public ConnectionAutoLoader(TransitDb db, TimeSpan triggerEvery,
            Func<DateTime, List<(DateTime start, DateTime end)>> generateWindow)
        {
            GenerateWindow = generateWindow;
            _db = db;
            _triggerEvery = triggerEvery;


            var timer = new Timer(triggerEvery.TotalMilliseconds);
            timer.Elapsed += Refresh;
            timer.Start();
            Log.Information($"Started autoloading timer with interval {triggerEvery}");

            Refresh(null, null);
        }

        private void Refresh(Object sender, ElapsedEventArgs eventArgs)
        {
            var unixNow = DateTime.Now.ToUnixTime();
            var date = unixNow - (ulong) (unixNow % _triggerEvery.TotalSeconds);
            var windows = GenerateWindow(date.FromUnixTime());

            foreach (var window in windows)
            {
                Log.Information($"Autoloading time period {window.start} -> {window.end}");
                _db.UpdateTimeFrame(window.start, window.end, refresh: true);
            }
        }
    }
}