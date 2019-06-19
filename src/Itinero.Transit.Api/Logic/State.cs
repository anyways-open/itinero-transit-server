using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    public class State
    {

        public static State GlobalState;
        
        // All the global state for the controllers goes here

        public const string Version = "Mostly finished (Itinero-transit 1.0.0-pre34)";

        /// <summary>
        /// The central transit DB where everyone refers to
        /// </summary>
        public readonly Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> TransitDbs;

        public string FreeMessage;

        // public static Synchronizer Synchronizer;

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


        public State(Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> transitDbs)
        {
            if (transitDbs.Count == 0)
            {
                throw new ArgumentException("No databases loaded. Is the internet connected?");
            }
            
            TransitDbs = transitDbs;
            BootTime = DateTime.Now;
            
        }

        public IStopsReader GetStopsReader()
        {
            var readers = All().Select(tdb =>
                (IStopsReader) tdb.StopsDb.GetReader()).ToList();
            return
                StopsReaderAggregator.CreateFrom(readers);
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