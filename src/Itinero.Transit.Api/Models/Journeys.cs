using System;
using System.Collections.Generic;

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A collection of journeys.
    /// </summary>
    public class Journeys
    {
        /// <summary>
        /// Creates a new journeys collection.
        /// </summary>
        /// <param name="journeys">The journeys.</param>
        internal Journeys(List<Journey> journeys)
        {
            ResultGeneratedTime = DateTime.Now;
            this.journeys = journeys;
        }

        /// <summary>
        /// The result generate time.
        /// </summary>
        public DateTime ResultGeneratedTime { get; }

        /// <summary>
        /// The journeys.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public List<Journey> journeys { get; }
    }
}