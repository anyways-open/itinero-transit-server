using System.Collections.Generic;
using Itinero.Transit.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A collection of locations.
    /// </summary>
    public class Locations
    {
        internal Locations(StopsDb.StopsDbReader reader, IEnumerable<IStop> stops)
        {
            this.stops = new List<Location>();
            foreach (var stop in stops)
            {
                reader.MoveTo(stop.Id);
                // For some reason I can't explain
                // using the enumerable directly doesn't always include the name of the stop
                this.stops.Add(new Location(reader)); 
            }
        }
        
        /// <summary>
        /// The stops.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public List<Location> stops { get; }
    }
}