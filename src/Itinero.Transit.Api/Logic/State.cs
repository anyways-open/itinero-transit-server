using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logging;
using Itinero.Transit.Api.Logic.Search;
using Itinero.Transit.Api.Logic.Transfers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    ///<summary>
    /// All the global state for the controllers goes here
    /// </summary>
    public class State
    {
        /// <summary>
        /// The singleton instance that everything uses
        /// Assigned by startup
        /// </summary>
        public static State GlobalState;

        public readonly RouterDb RouterDb;

        public const string VersionNr = "1.0.0-pre91";

        /// <summary>
        /// A version information string - useful to see what version is in production.
        /// The first letter of the word is increased alphabetically
        /// </summary>
        public const string Version =
            "Questioning multiple operators (Itinero-transit 1.0.0-pre91, Routable-Tiles pre33, server " +
            VersionNr + ")";


        /// <summary>
        /// A log message. Startup can assign this message freely.
        /// </summary>
        public string FreeMessage;

        public readonly FileLogger Logger;


        /// <summary>
        /// Keeps a Trie (prefixtree) to quickly do a somewhat fuzzy search. 
        /// </summary>
        public NameIndex NameIndex;

        /// <summary>
        /// How important is each station?
        /// Maps URI -> Number of trains stopping
        /// </summary>
        public Dictionary<string, uint> Importances;

        /// <summary>
        /// How important is each station?
        /// Maps InternalID -> Number of trains stopping
        /// </summary>
        public Dictionary<StopId, uint> ImportancesInternal;


        /// <summary>
        /// When did the server start?
        /// </summary>
        public readonly DateTime BootTime;

        /// <summary>
        /// What kinds of walk-generators are available?
        /// </summary>
        public readonly OtherModeBuilder OtherModeBuilder;

        public readonly OperatorManager Operators;

        public State(IEnumerable<Operator> operators,
            OtherModeBuilder otherModeBuilder, RouterDb routerDb, FileLogger logger)
        {
            Operators = new OperatorManager(operators);
            if (!Operators.All.Any())
            {
                throw new ArgumentException(
                    "No databases loaded. Is the internet connected? Is the configuration correct?");
            }

            OtherModeBuilder = otherModeBuilder;
            RouterDb = routerDb;
            Logger = logger;
            BootTime = DateTime.Now;
        }
    }

    public struct Operator
    {
        public readonly uint MaxSearch;
        public readonly TransitDb Tdb;
        public readonly Synchronizer Synchronizer;
        public readonly string Name;
        public readonly HashSet<string> AltNames;
        public readonly HashSet<string> Tags;

        public Operator(string name, TransitDb tdb, Synchronizer synchronizer,
            uint maxSearch,
            IEnumerable<string> altNames,
            IEnumerable<string> tags)
        {
            MaxSearch = maxSearch;
            Name = name;
            Tdb = tdb;
            Synchronizer = synchronizer;
            AltNames = altNames?.ToHashSet() ?? new HashSet<string>();
            Tags = tags?.ToHashSet() ?? new HashSet<string>();
        }

    }
}