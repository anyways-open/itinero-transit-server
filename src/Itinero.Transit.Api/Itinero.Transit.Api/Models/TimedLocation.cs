using System;

// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Itinero.Transit.Api.Models
{
    public class TimedLocation
    {
        public readonly Location Location;
        public readonly DateTime Time;


        public TimedLocation(Location location, ulong time)
            : this(location, time.FromUnixTime())
        {
        }

        public TimedLocation(Location location, DateTime time)
        {
            Location = location;
            Time = time;
        }
    }
}