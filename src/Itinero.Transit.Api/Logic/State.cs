using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.IO.OSM.Data;
using Serilog;

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


        /// <summary>
        /// A version information string - useful to see what version is in production.
        /// The first letter of the word is increased alphabetically
        /// </summary>
        public const string Version = "Neophyte (Itinero-transit 1.0.0-pre34)";

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
        public Dictionary<LocationId, uint> ImportancesInternal;


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
            OtherModeBuilder otherModeBuilder)
        {
            if (transitDbs.Count == 0)
            {
                throw new ArgumentException("No databases loaded. Is the internet connected?");
            }

            TransitDbs = transitDbs;
            OtherModeBuilder = otherModeBuilder;
            BootTime = DateTime.Now;
        }


        /// <summary>
        /// Get a stops reader for all the loaded databases.
        /// Note that we keep a few stopsreaders around to provide multiple levels of caching.
        /// If searchRange == 0, that means that no caching is needed
        /// </summary>
        /// <returns></returns>
        public IStopsReader GetStopsReader(uint searchRange)
        {
            if (_stopsReaderCloseLocationsCached.ContainsKey(searchRange))
            {
                // TODO FIXME BUG This might not be threadsafe
                // Two threads could possible have the same stopsreader at the same moment and move them to different stops
                // The read them getting wrong locations...
                return _stopsReaderCloseLocationsCached[searchRange];
            }


            // Create the reader for the StopDb
            var reader = StopsReaderAggregator.CreateFrom(
                All().Select(tdb =>
                    (IStopsReader) tdb.StopsDb.GetReader()).ToList());

            if (searchRange > 0)
            {
                // Apply caching
                reader = reader.UseCache();

                var start = DateTime.Now;
                reader.Reset();
                while (reader.MoveNext())
                {
                    var current = (IStop) reader;
                    // For each stop, determine what locations are in range.
                    // Although the result is thrown away here, it is cached as well
                    reader.LocationsInRange(
                        current.Latitude, current.Longitude,
                        searchRange);
                }

                var end = DateTime.Now;
                Log.Information($"Caching stops within range {searchRange} took {(end - start).TotalMilliseconds}ms");
            }

            // And throw in an extra (non-cached) OSM-reader to be able to parse floating URLS
            reader = StopsReaderAggregator.CreateFrom(
                new List<IStopsReader>
                {
                    reader,
                    new OsmLocationStopReader(
                        (uint) TransitDbs.Count)
                });

            // All set!
            _stopsReaderCloseLocationsCached[searchRange] = reader;

            return reader;
        }

        public IConnectionReader GetConnectionsReader()
        {
            var readers = All().Select(tdb =>
                (IConnectionReader) tdb.ConnectionsDb.GetReader()).ToList();

            return ConnectionReaderAggregator.CreateFrom(readers);
        }

        public IConnectionEnumerator GetConnections()
        {
            var readers = All().Select(tdb =>
                (IConnectionEnumerator) tdb.ConnectionsDb.GetDepartureEnumerator()).ToList();

            return ConnectionEnumeratorAggregator.CreateFrom(readers);
        }


        public ITripReader GetTripsReader()
        {
            var readers =
                All().Select(tdb => (ITripReader) tdb.TripsDb.GetReader());
            return TripReaderAggregator.CreateFrom(readers);
        }

        public IEnumerable<TransitDb.TransitDbSnapShot> All()
        {
            return TransitDbs.Select(v => v.Value.tdb.Latest);
        }

      
    }
}