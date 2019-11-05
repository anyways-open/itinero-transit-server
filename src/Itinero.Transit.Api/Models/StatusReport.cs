using System;
using System.Collections.Generic;
using System.Diagnostics;

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
        public readonly DateTime OnlineSince;

        /// <summary>
        /// The time (in seconds) that the server has been running
        /// </summary>
        public long Uptime;

        /// <summary>
        /// Indicates what time fragments are loaded into the database.
        /// This is a list of (start, end) values
        /// </summary>
        public readonly Dictionary<string, OperatorStatus> LoadedOperators;


        /// <summary>
        /// A small string so that the programmer knows a little what version is running.
        /// Should be taken with a grain of salt
        /// </summary>
        public readonly string Version;

        public readonly List<string> SupportedProfiles;
        public readonly List<string> SupportedOsmProfiles;

        public readonly Dictionary<string, string> CurrentRunningTask;

        /// <summary>
        /// Indicates how many routable tiles are cached on the disk. 
        /// </summary>
        public uint TilesOnDisk;

        /// <summary>
        /// Memory usage of the service, in bytes consumed
        /// </summary>
        public long BytesUsed;

        public long MegabytesUsed;

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

        public TimeWindow((DateTime, DateTime) t) : this(t.Item1, t.Item2)
        {
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