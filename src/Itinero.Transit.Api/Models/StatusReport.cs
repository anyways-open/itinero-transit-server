using System;
using System.Collections.Generic;
// ReSharper disable NotAccessedField.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// The status report gives some insight in the server.
    /// </summary>
    public class StatusReport
    {
        /// <summary>
        /// Indicates if the server is online
        /// </summary>
        public readonly bool Online = true;

        /// <summary>
        /// When the server got online
        /// </summary>
        public DateTime OnlineSince;

        /// <summary>
        /// The time (in seconds) that the server has been running
        /// </summary>
        public long Uptime;
        
        /// <summary>
        /// Indicates what time fragments are loaded into the database.
        /// This is a list of (start, end) values
        /// </summary>
        public readonly List<TimeWindow> LoadedTimeWindows;


        /// <summary>
        /// A small string so that the programmer knows a little what version is running.
        /// Should be taken with a grain of salt
        /// </summary>
        public readonly string Version;

        public readonly string CurrentRunningTask;


        public StatusReport(DateTime onlineSince, long uptime,
            IEnumerable<(DateTime start, DateTime end)> loadedTimeWindows,
            string version, string currentRunningTask)
        {
            OnlineSince = onlineSince;
            Uptime = uptime;
            LoadedTimeWindows = new List<TimeWindow>();
            foreach (var w in loadedTimeWindows)
            {
                LoadedTimeWindows.Add(new TimeWindow(w));
            }

            Version = version;
            CurrentRunningTask = currentRunningTask;
        }

        public class TimeWindow
        {
            public readonly DateTime Start, End;

            public TimeWindow(DateTime start, DateTime end)
            {
                Start = start;
                End = end;
            }

            public TimeWindow((DateTime, DateTime) t) : this(t.Item1, t.Item2)
            {
            }
        }
    }
}