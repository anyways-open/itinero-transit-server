using System.Collections.Generic;
using Itinero.Transit.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api.Models
{
    public class LocationResult
    {
        // ReSharper disable once InconsistentNaming
        public readonly string Id, Name;
        public readonly int Difference;

        public LocationResult(string id, string name, int difference)
        {
            Id = id;
            Name = name;
            Difference = difference;
        }
    }
}