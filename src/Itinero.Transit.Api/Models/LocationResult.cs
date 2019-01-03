using System.Collections.Generic;
using Itinero.Transit.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// Result when querying a location
    /// </summary>
    public class LocationResult
    {
        // ReSharper disable once InconsistentNaming
        public readonly Location Location;

        /// <summary>
        /// Indicates how bad the match is.
        /// 0 is a perfect match. The bigger the number, the less likely the match.
        /// </summary>
        public readonly int Difference;

        /// <summary>
        /// Gives an indication how important a station is. The higher the number, the more trains pass there.
        /// Can be used to compare two value to each other.
        /// Might be deprecated in the future
        /// Calculation method might change in the future.
        /// Make sure your code can handle sudden changes or drops of this field
        /// </summary>
        public readonly uint Importance;

        public LocationResult( Location location, int difference, uint importance)
        {
            Location = location;
            Difference = difference;
            Importance = importance;
        }
    }
}