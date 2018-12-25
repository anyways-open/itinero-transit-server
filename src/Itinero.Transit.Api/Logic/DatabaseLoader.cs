// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;
using Serilog;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Bundles all the needed databases, and offers a default database for belgium
    /// </summary>
    public class DatabaseLoader
    {
        public readonly StopsDb Stops;
        public readonly ConnectionsDb Connections;
        public readonly TripsDb Trips;
        public readonly Profile Profile;
        public readonly Dictionary<string, uint> ConnectionCounts;

        public DatabaseLoader(StopsDb stops, ConnectionsDb connections, TripsDb trips, Profile profile, Dictionary<string, uint> connectionCounts)
        {
            Stops = stops;
            Connections = connections;
            Trips = trips;
            Profile = profile;
            ConnectionCounts = connectionCounts;
        }


        public static DatabaseLoader Belgium()
        {
            var sncb = IO.LC.CSA.Belgium.Sncb(new LocalStorage("cache"));

            var stopsDb = new StopsDb();
            var connectionsDb = new ConnectionsDb();
            var tripsDb = new TripsDb();

            var timeWindow = (DateTime.Now.Date, TimeSpan.FromDays(3));

            var treader = tripsDb.GetReader();
      
            
            connectionsDb.LoadConnections(sncb, stopsDb, tripsDb, timeWindow);
            

            var counts = new Dictionary<string, uint>();
            
            var cons = connectionsDb.GetDepartureEnumerator();
            var stopsreader = stopsDb.GetReader();
            while (cons.MoveNext())
            {
                stopsreader.MoveTo(cons.DepartureStop);
                Increase(counts, stopsreader.GlobalId);
                stopsreader.MoveTo(cons.ArrivalStop);
                Increase(counts, stopsreader.GlobalId);
            }
            
            
            return new DatabaseLoader(stopsDb, connectionsDb, tripsDb, sncb, counts);
        }

        private static void Increase<TK>(Dictionary<TK, uint> d, TK k)
        {
            if (!d.ContainsKey(k))
            {
                d[k] = 1;
            }
            else
            {
                d[k]++;
            }
        }
    }
}