using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    public class Databases
    {
        public Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> TransitDbs;

        public Databases(Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> transitDbs)
        {
            if (transitDbs.Count == 0)
            {
                throw new ArgumentException("No databases loaded. Is the internet connected?");
            }
            TransitDbs = transitDbs;
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
            return  TripReaderAggregator.CreateFrom(readers);
        }

        public IEnumerable<TransitDb.TransitDbSnapShot> All()
        {
            return TransitDbs.Select(v => v.Value.tdb.Latest);
        }
    }

    public class State
    {
        // All the global state for the controllers goes here

        /// <summary>
        /// The central transit DB where everyone refers to
        /// </summary>
        public static Databases TransitDb;

        public static string FreeMessage;

        // public static Synchronizer Synchronizer;

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

        public const string Version = "Lions (Itinero-transit 1.0.0-pre25)";

        public static IEnumerable<TransitDb.TransitDbSnapShot> TransitDbs()
        {
            return TransitDb.All();
        }
    }
}