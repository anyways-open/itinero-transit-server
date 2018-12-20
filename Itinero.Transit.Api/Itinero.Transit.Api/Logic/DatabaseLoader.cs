// ReSharper disable MemberCanBePrivate.Global

using System;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Attributes;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;
using Serilog;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.Api.Logic
{
    public class DatabaseLoader
    {
        public readonly StopsDb Stops;
        public readonly ConnectionsDb Connections;
        public readonly TripsDb Trips;

        public DatabaseLoader(StopsDb stops, ConnectionsDb connections, TripsDb trips)
        {
            Stops = stops;
            Connections = connections;
            Trips = trips;
        }


        public static DatabaseLoader Belgium()
        {
            var sncb = IO.LC.CSA.Belgium.Sncb(new IO.LC.CSA.Utils.LocalStorage("cache"));

            var stopsDb = new StopsDb();
            var connectionsDb = new ConnectionsDb();
            var tripsDb = new TripsDb();

            var timeWindow = (DateTime.Now.Date, TimeSpan.FromDays(1));

            foreach (var l in sncb.GetAllLocations())
            {
                stopsDb.Add(l.Id().ToString(), l.Lon, l.Lat,new Attribute("name", l.Name));
                Log.Information($"Added {l.Uri}");
            }

            connectionsDb.LoadConnections(sncb, stopsDb, tripsDb, timeWindow);

            return new DatabaseLoader(stopsDb, connectionsDb, tripsDb);
        }
    }
}