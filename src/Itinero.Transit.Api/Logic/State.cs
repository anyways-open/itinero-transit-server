using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    public class State
    {
        // All the global state for the controllers goes here

        /// <summary>
        /// The central transit DB where everyone refers to
        /// </summary>
        public static TransitDb TransitDb;

        
        public static Synchronizer Synchronizer;

        public static NameIndex NameIndex;
        
        /// <summary>
        /// How important is each station?
        /// Maps URI -> Number of trains stopping
        /// </summary>
        public static Dictionary<string, uint> Importances;
        
        /// <summary>
        /// How important is each station?
        /// Maps InternalID -> Number of trains stopping
        /// </summary>
        public static Dictionary<LocationId, uint> ImportancesInternal;


        /// <summary>
        /// When did the server start?
        /// </summary>
        public static DateTime BootTime;

        public const string Version = "Kittens (Itinero-transit 1.0.0-pre8)";

    }
}