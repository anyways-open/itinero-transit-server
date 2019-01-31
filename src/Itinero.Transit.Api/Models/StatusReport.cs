using System;
using System.Collections.Generic;
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
        public const bool Online = true;

        /// <summary>
        /// When the server got online
        /// </summary>
        public DateTime OnlineSince { get; }

        /// <summary>
        /// The time (in seconds) that the server has been running
        /// </summary>
        public long Uptime { get; }

        /// <summary>
        /// Indicates what time fragments are loaded into the database.
        /// This is a list of (start, end) values
        /// </summary>
        public readonly List<(DateTime start, DateTime end)> LoadedTimeWindows;

        
        /// <summary>
        /// A small string so that the programmer knows a little what version is running.
        /// Should be taken with a grain of salt
        /// </summary>
        public readonly string Version;


        public StatusReport(DateTime onlineSince, long uptime, List<(DateTime start, DateTime end)> loadedTimeWindows, string version)
        {
            OnlineSince = onlineSince;
            Uptime = uptime;
            LoadedTimeWindows = loadedTimeWindows;
            Version = version;
        }
    }
}