// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;

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
        private readonly Action _loadLocations;
        private readonly Action<DateTime, DateTime> _loadTimeWindow;

        public readonly Status Status;

        public DatabaseLoader(StopsDb stops, ConnectionsDb connections, TripsDb trips, Profile profile,
            Dictionary<string, uint> connectionCounts,
            Action loadLocations,
            Action<DateTime, DateTime> loadTimeWindow, Status status)
        {
            Stops = stops;
            Connections = connections;
            Trips = trips;
            Profile = profile;
            ConnectionCounts = connectionCounts;
            _loadLocations = loadLocations;
            _loadTimeWindow = loadTimeWindow;
            Status = status;
        }


        public static DatabaseLoader Belgium()
        {
            var sncb = IO.LC.CSA.Belgium.Sncb(new LocalStorage("cache"));

            var stopsDb = new StopsDb();
            var connectionsDb = new ConnectionsDb();
            var tripsDb = new TripsDb();


            var counts = new Dictionary<string, uint>();

            var status = new Status(DateTime.MaxValue, DateTime.MinValue, DateTime.MinValue);

            var loadLocations = new Action(() =>
            {
                stopsDb.LoadLocations(sncb, (loaded, total) =>
                {
                    status.LoadedLocationsCount = loaded;
                    status.TargetLocationsCount = total;
                });
            });


            var loadWindow = new Action<DateTime, DateTime>
            ((start, end) =>
            {
                
                status.TargetLastDeparture = end;
                connectionsDb.LoadConnections(sncb, stopsDb, tripsDb, (start, end-start),
                    connection =>
                    {
                        Increase(counts, connection.DepartureLocation().ToString());
                        Increase(counts, connection.ArrivalLocation().ToString());
                    } ,
                    (loadedConnections, lastDepTime, factor) =>
                    {
                        status.FirstDepartureTime = start;
                        status.LoadedConnectionsCount = loadedConnections;
                        status.LastDepartureTime = lastDepTime;
                    });
            });


            return new DatabaseLoader(stopsDb, connectionsDb, tripsDb, sncb, counts, loadLocations, loadWindow, status);
        }

        public void LoadLocations()
        {
            _loadLocations.Invoke();
        }

        public void LoadTimeWindow(DateTime start, DateTime end)
        {
            _loadTimeWindow(start, end);
        }


        private static void Increase<TK>(IDictionary<TK, uint> d, TK k)
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

    public class Status
    {
        public DateTime FirstDepartureTime, LastDepartureTime, TargetLastDeparture;
        public int LoadedLocationsCount, TargetLocationsCount, LoadedConnectionsCount;

        public Status(DateTime firstDepartureTime, DateTime lastDepartureTime, DateTime targetLastDeparture)
        {
            FirstDepartureTime = firstDepartureTime;
            LastDepartureTime = lastDepartureTime;
            TargetLastDeparture = targetLastDeparture;
        }
    }
}