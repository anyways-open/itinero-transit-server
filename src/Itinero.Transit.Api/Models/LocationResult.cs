using System.Collections.Generic;
using Itinero.Transit.Data;

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// Result when querying a location
    /// </summary>
    public class LocationResult
    {
        internal LocationResult( Location location, int difference, uint importance)
        {
            Location = location;
            Difference = difference;
            Importance = importance;
        }
        
        /// <summary>
        /// The location.
        /// </summary>
        public Location Location { get; }

        /// <summary>
        /// The difference.
        /// </summary>
        /// <remarks>
        /// Indicates how bad the match is.
        /// 0 is a perfect match. The bigger the number, the less likely the match.
        /// </remarks>
        public int Difference { get; }
        
        /// <summary>
        /// Gives an indication how important a station is. The higher the number, the more trains pass there.
        /// </summary>
        /// <remarks>
        /// Can be used to compare two value to each other.
        /// Might be deprecated in the future
        /// Calculation method might change in the future.
        /// Make sure your code can handle sudden changes or drops of this field
        /// </remarks>
        public uint Importance { get; }
    }
}