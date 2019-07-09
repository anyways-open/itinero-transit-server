// ReSharper disable NotAccessedField.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;

namespace Itinero.Transit.Api.Models
{
    /// <summary>
    /// A segment represents a part of the journey, e.g. one link where you take the train    
    /// </summary>
    public class Segment
    {
        internal Segment(TimedLocation departure, TimedLocation arrival, string vehicle, string headSign)
        {
            Departure = departure;
            Arrival = arrival;
            Vehicle = vehicle;
            Headsign = headSign;
            if (Vehicle == null)
            {
                throw new ArgumentNullException(nameof(Vehicle));
            }

            Generator = null;
            Coordinates = null;
        }

        internal Segment(TimedLocation departure, TimedLocation arrival, string generator, List<Coordinate> coordinates)
        {
            Departure = departure;
            Arrival = arrival;
            Vehicle = null;
            Headsign = null;
            Generator = generator;
            Coordinates = coordinates;
        }

        /// <summary>
        /// The departure location.
        /// </summary>
        public TimedLocation Departure { get; }

        /// <summary>
        /// The arrival location.
        /// </summary>
        public TimedLocation Arrival { get; }

        /// <summary>
        /// The identifier of the vehicle (the global trip id).
        /// Meant for machines
        /// </summary>
        public string Vehicle { get; }

        /// <summary>
        /// The name of the train, e.g. its destination.
        /// Meant for humans
        /// </summary>
        public string Headsign { get; }

        public string Generator { get; }
        public List<Coordinate> Coordinates { get; }
    }
}