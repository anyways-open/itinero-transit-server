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
        /// The route that the traveller should take if she'd go directly.
        /// This route follows the constraints specified by the walksGenerator.
        /// </summary>
        public Segment DirectWalk { get; }

        /// <summary>
        /// A list of walks, which are referenced by the journeys.
        /// This compresses multiple returning walks
        /// </summary>
        public List<List<Coordinate>> CommonCoordinates { get; }

        /// <summary>
        /// When the query was started
        /// </summary>
        public DateTime QueryStarted { get; }

        /// <summary>
        /// When the query was completed
        /// </summary>
        public DateTime QueryDone { get; }

        /// <summary>
        /// How much milliseconds the query took to run
        /// </summary>
        public uint RunningTime { get; }

        /// <summary>
        /// Gives the description of how walking in between stops was interpreted
        /// </summary>
        public string WalksDescription { get; }

        /// <summary>
        /// Start of the timewindow that was searched for this query
        /// </summary>
        public DateTime EarliestDeparture { get; }

        /// <summary>
        /// End of the timewindow that was searched for this query
        /// </summary>
        public DateTime LatestArrival { get; }

        internal QueryResult(List<Journey> journeys, DateTime queryStarted, DateTime queryDone,
            DateTime earliestDeparture, DateTime latestArrival, string walksDescription, Segment directWalk)
        {
            Journeys = journeys;
            QueryStarted = queryStarted;
            QueryDone = queryDone;
            EarliestDeparture = earliestDeparture;
            LatestArrival = latestArrival;
            WalksDescription = walksDescription;
            DirectWalk = directWalk;
            RunningTime = (uint) (queryDone - queryStarted).TotalMilliseconds;
        }
    }
}