using System;
using Itinero.Transit.Utils;
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A location and time.
    /// </summary>
    public class TimedLocation
    {
        internal TimedLocation(Location location, ulong time, uint delay)
            : this(location, time.FromUnixTime(), delay)
        {
            
        }

        internal TimedLocation(Location location, DateTime? time, uint delay)
        {
            Location = location;
            Time = time;
            Delay = delay;
            PlannedTime = null;
            if (time != null)
            {
              PlannedTime =  time - TimeSpan.FromSeconds(delay);
            }
        }
        
        /// <summary>
        /// The location.
        /// </summary>
        public Location Location { get; }
        
        /// <summary>
        /// The time of departure/arrival.
        /// </summary>
        public DateTime? Time { get; }
        
        /// <summary>
        /// The planned time of departure/arrival
        /// </summary>
        public DateTime? PlannedTime { get;  }
        
        
        /// <summary>
        /// The delay on departure/arrival
        /// Note that the delay is _included_ into time.
        /// PlannedTime + Delay = Time
        /// </summary>
        public uint Delay { get; }
    }
}