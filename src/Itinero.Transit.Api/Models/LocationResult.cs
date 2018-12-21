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
        public readonly string Id, Name;
        
        /// <summary>
        /// Indicates how bad the match is.
        /// 0 is a perfect match. The bigger the number, the less likely the match.
        /// </summary>
        public readonly int Difference;

        public LocationResult(string id, string name, int difference)
        {
            Id = id;
            Name = name;
            Difference = difference;
        }
    }
}