using System;
using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// Represents the result of a query: all the possible journeys the traveller could take + some metadata
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// The journeys which the traveller could take
        /// </summary>
        public List<Journey> Journeys { get; }

        /// <summary>
        /// When the query was completed
        /// </summary>
        public DateTime QueryStarted { get; }

        public DateTime QueryDone { get; }

        /// <summary>
        /// How much milliseconds the query took to run
        /// </summary>
        public uint RunningTime { get; }

        /// <summary>
        /// Start of the timewindow that was searched for this query
        /// </summary>
        public DateTime EarliestDeparture { get; }

        /// <summary>
        /// End of the timewindow that was searched for this query
        /// </summary>
        public DateTime LatestArrival { get; }

        internal QueryResult(List<Journey> journeys, DateTime queryStarted, DateTime queryDone,
            DateTime earliestDeparture, DateTime latestArrival)
        {
            Journeys = journeys;
            QueryStarted = queryStarted;
            QueryDone = queryDone;
            EarliestDeparture = earliestDeparture;
            LatestArrival = latestArrival;
            RunningTime = (uint) (queryDone - queryStarted).TotalMilliseconds;
        }
    }
}