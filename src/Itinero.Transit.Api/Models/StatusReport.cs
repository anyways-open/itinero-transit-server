using System;

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// The status report gives some insight in the server.
    /// </summary>
    public class StatusReport
    {
        internal StatusReport(DateTime onlineSince, long uptime, int loadedLocations, int targetLocationsCount,
            int loadedConnections,
            DateTime firstConnectionDeparture, DateTime lastConnectionDeparture, DateTime targetLastConnectionDeparture)
        {
            OnlineSince = onlineSince;
            Uptime = uptime;
            LoadedLocationsCount = loadedLocations;
            TargetLocationsCount = targetLocationsCount;
            LoadedConnections = loadedConnections;
            FirstConnectionDeparture = firstConnectionDeparture;
            LastConnectionDeparture = lastConnectionDeparture;
            TargetLastConnectionDeparture = targetLastConnectionDeparture;
            var factor = (float) ((lastConnectionDeparture - firstConnectionDeparture).TotalSeconds /
                                  (targetLastConnectionDeparture - firstConnectionDeparture).TotalSeconds);
            var percentPerSecond = factor / uptime;
            EstimatedTimeLeft = (int) ((1 - factor) / percentPerSecond);

            PercentageLoaded = 100 * factor;
        }
        
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
        /// Indicates how many stop locations are loaded in the database
        /// </summary>
        public int LoadedLocationsCount { get; }

        /// <summary>
        /// The expected number of locations that will be loaded in the database when done
        /// </summary>
        public int TargetLocationsCount { get; }

        /// <summary>
        /// Indicates how many connections are loaded in the database.
        /// A public transport vehicle which depart at stop A and arrives at stop B without intermediate stop is represented by a connection.
        /// Of course, a PT-vehicle will do multiple stops per route. We represent those as multiple connections.
        /// </summary>
        public int LoadedConnections { get; }

        /// <summary>
        /// The departure time of the first connection that is loaded in the database.
        /// Hence, it has no use to query for journeys before this point in time
        /// </summary>
        public DateTime FirstConnectionDeparture { get; }

        /// <summary>
        /// The departure time of the last connection that is loaded in the database.
        /// Hence, it has no use to query for journeys after this point in time
        /// </summary>
        public DateTime LastConnectionDeparture { get; }

        /// <summary>
        /// The departure time of the last connection we want to load into the database.
        /// In other words, once the database will be fully loaded, `TargetLastConnectionDeparture` will equal `LastConnectionDeparture`
        /// </summary>
        public DateTime TargetLastConnectionDeparture { get; }

        /// <summary>
        /// The estimate percentage of loaded connections
        /// </summary>
        public float PercentageLoaded { get; }

        /// <summary>
        /// Estimates how many seconds that are still needed to load the entire database
        /// </summary>
        public int EstimatedTimeLeft { get; }
    }
}