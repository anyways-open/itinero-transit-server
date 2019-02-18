using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;

namespace Itinero.Transit.Api.Logic
{
    public class State
    {
        // All the global state for the controllers goes here

        /// <summary>
        /// The central transit DB where everyone refers to
        /// </summary>
        public static TransitDb TransitDb;

        public static JourneyTranslator JourneyTranslator;

        public static LinkedConnectionDataset LcProfile;

        /// <summary>
        /// How important is each station?
        /// Maps URI -> Number of trains stopping
        /// </summary>
        public static Dictionary<string, uint> Importances;

        /// <summary>
        /// When did the server start?
        /// </summary>
        public static DateTime BootTime;

        public const string Version = "Disable CORS";


        public static (DateTime start, DateTime end, double percetage)? CurrentlyLoadingWindow;
    }
}