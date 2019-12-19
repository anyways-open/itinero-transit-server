using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

// ReSharper disable NotAccessedField.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// The status report gives some insight in the server.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class StatusReport
    {
        /// <summary>
        /// Indicates if the server is online
        /// </summary>
        public bool Online => true;

        /// <summary>
        /// When the server got online
        /// </summary>
        public DateTime OnlineSince { get; }

        /// <summary>
        /// The time (in seconds) that the server has been running
        /// </summary>
        public long Uptime { get;  };

        /// <summary>
        /// Indicates what time fragments are loaded into the database.
        /// This is a list of (start, end) values
        /// </summary>
        public Dictionary<string, OperatorStatus> LoadedOperators { get; }


        /// <summary>
        /// A small string so that the programmer knows a little what version is running.
        /// Should be taken with a grain of salt
        /// </summary>
        public string Version { get; }

        public List<string> SupportedProfiles { get; }
        public List<string> SupportedOsmProfiles { get; }

        public Dictionary<string, string> CurrentRunningTask { get; }

        /// <summary>
        /// Indicates how many routable tiles are cached on the disk. 
        /// </summary>
        public uint TilesOnDisk { get; }

        /// <summary>
        /// Memory usage of the service, in bytes consumed
        /// </summary>
        public long BytesUsed { get; }

        public long MegabytesUsed { get; }

        public StatusReport(DateTime onlineSince, long uptime,
            Dictionary<string, OperatorStatus> operatorStatuses,
            string version, Dictionary<string, string> currentRunningTask, List<string> supportedProfiles,
            List<string> supportedOsmProfiles, uint tilesOnDisk)
        {
            TilesOnDisk = tilesOnDisk;
            OnlineSince = onlineSince;
            Uptime = uptime;
            LoadedOperators = operatorStatuses;

            Version = version;
            CurrentRunningTask = currentRunningTask;
            SupportedProfiles = supportedProfiles;
            SupportedOsmProfiles = supportedOsmProfiles;
            using (var proc = Process.GetCurrentProcess())
            {
                BytesUsed = proc.WorkingSet64;
            }

            MegabytesUsed = BytesUsed / (1024 * 1024);
        }
    }

    public class TimeWindow
    {
        public readonly DateTime Start, End;

        public TimeWindow(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }
    }

    public class OperatorStatus
    {
        public readonly List<string> AltNames, Tags;
        public readonly TimeWindow LoadedTimeWindows;

        public OperatorStatus(List<string> altNames, List<string> tags, TimeWindow loadedTimeWindow)
        {
            AltNames = altNames;
            Tags = tags;
            LoadedTimeWindows = loadedTimeWindow;
        }
    }
}