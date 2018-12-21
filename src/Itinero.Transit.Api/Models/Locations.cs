using System.Collections.Generic;
using Itinero.Transit.Data;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global

namespace Itinero.Transit.Api.Models
{
    public class Locations
    {
        // ReSharper disable once InconsistentNaming
        public readonly List<Location> stops;

        public Locations(StopsDb.StopsDbReader reader, IEnumerable<IStop> stops)
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
    }
}