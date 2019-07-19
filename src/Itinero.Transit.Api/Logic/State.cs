using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.IO.OSM.Data;

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


        /// <summary>
        /// A version information string - useful to see what version is in production.
        /// The first letter of the word is increased alphabetically
        /// </summary>
        public const string Version = "Osoc Inching closer (Itinero-transit 1.0.0-pre65)";

        /// <summary>
        /// This dictionary contains all the loaded transitDbs, indexed on their name.
        /// The object responsible of regularly reloading them is included too
        /// </summary>
        public readonly Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> TransitDbs;

        /// <summary>
        /// A log message. Startup can assign this message freely.
        /// </summary>
        public string FreeMessage;


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


        private readonly Dictionary<uint, IStopsReader> _stopsReaderCloseLocationsCached
            = new Dictionary<uint, IStopsReader>();

        public State(Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> transitDbs,
            OtherModeBuilder otherModeBuilder, RouterDb routerDb)
        {
            if (transitDbs.Count == 0)
            {
                throw new ArgumentException("No databases loaded. Is the internet connected?");
            }

            TransitDbs = transitDbs;
            OtherModeBuilder = otherModeBuilder;
            RouterDb = routerDb;
            BootTime = DateTime.Now;
        }

        private IStopsReader cachedStopsReader, cachedStopsReaderOsm;

        /// <summary>
        /// Get a stops reader for all the loaded databases.
        /// </summary>
        /// <returns></returns>
        public IStopsReader GetStopsReader(bool withOsm)
        {
            if (!withOsm)
            {
                if (cachedStopsReader == null)
                {
                    cachedStopsReader = StopsReaderAggregator.CreateFrom(
                            All().Select(tdb => (IStopsReader) tdb.StopsDb.GetReader()).ToList())
                        .UseCache();
                }

                return cachedStopsReader;
            }
            // ReSharper disable once RedundantIfElseBlock
            else
            {
                if (cachedStopsReaderOsm == null)
                {
                    var reader = GetStopsReader(false);
                    var osm = new OsmLocationStopReader(
                        reader.DatabaseIndexes().Max() + 1u);
                    cachedStopsReaderOsm = StopsReaderAggregator.CreateFrom(new List<IStopsReader>
                    {
                        reader, osm
                    });
                }

                return cachedStopsReaderOsm;
            }
        }

        public IDatabaseReader<ConnectionId, Connection> GetConnectionsReader()
        {
            var readers = All().Select(tdb => tdb.ConnectionsDb).ToList();

            return DatabaseEnumeratorAggregator<ConnectionId, Connection>.CreateFrom(readers);
        }

        public IConnectionEnumerator GetConnections()
        {
            var readers = All().Select(tdb =>
                (IConnectionEnumerator) tdb.ConnectionsDb.GetDepartureEnumerator()).ToList();

            return ConnectionEnumeratorAggregator.CreateFrom(readers);
        }


        public IDatabaseReader<TripId, Trip> GetTripsReader()
        {
            var readers =
                All().Select(tdb => tdb.TripsDb);
            return DatabaseEnumeratorAggregator<TripId, Trip>.CreateFrom(readers);
        }

        public IEnumerable<TransitDb.TransitDbSnapShot> All()
        {
            return TransitDbs.Select(v => v.Value.tdb.Latest);
        }
    }
}