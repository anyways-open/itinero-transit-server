using System;

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A location and time.
    /// </summary>
    public class TimedLocation
    {
        internal TimedLocation(Location location, ulong time)
            : this(location, time.FromUnixTime())
        {
            
        }

        internal TimedLocation(Location location, DateTime time)
        {
            Location = location;
            Time = time;
        }
        
        /// <summary>
        /// The location.
        /// </summary>
        public Location Location { get; }
        
        /// <summary>
        /// The time.
        /// </summary>
        public DateTime Time { get; }
    }
}