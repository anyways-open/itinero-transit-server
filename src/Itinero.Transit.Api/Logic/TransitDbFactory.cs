using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.Synchronization;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    public static class TransitDbFactory
    {
        /// <summary>
        /// Builds the entire transitDB based on the relevant configuration section
        /// </summary>
        public static Databases CreateTransitDbs(
            this IConfiguration configuration, bool dryRun = false)
        {
            var dbs = new Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)>();
            uint id = 0;
            foreach (var config in configuration.GetChildren())
            {
                try
                {
                    var (name, db, synch) = config.CreateTransitDb(id, dryRun);
                    dbs.Add(name, (db, synch));
                    id++;
                }
                catch (Exception e)
                {
                    Log.Error($"Could not load database with config {config}\n" +
                              $"Probable cause: upstream server is offline (or internet is down)\n" +
                              $"{e}");
                }
            }

            return new Databases(dbs);
        }

        /// <summary>
        /// Builds the entire transitDB based on the relevant configuration section
        /// </summary>
        private static (String name, TransitDb db, Synchronizer synchronizer) CreateTransitDb(
            this IConfiguration configuration, uint id, bool dryRun = false)
        {
            var name = configuration.GetValue<string>("Name");


            var reloadingPolicies =
                configuration.GetSection("ReloadPolicy")
                    .GetSection("Windows")
                    .GetSynchronizedWindows();

            var cacheLocation = configuration.GetValue<string>("Cache");
            var cacheReload = configuration.GetValue("CacheUpdateEvery", long.MaxValue);

            if (dryRun)
            {
                // Only test config file
                return ("", null, null);
            }


            TransitDb db = null;
            if (!string.IsNullOrEmpty(cacheLocation))
            {
                db = TryLoadFromDisk(cacheLocation, id);
            }

            db = db ?? new TransitDb(id);


            Synchronizer synchronizer = null;

            reloadingPolicies.Add(new ImportanceCounter());

            if (cacheReload > 0 && cacheReload < long.MaxValue)
            {
                reloadingPolicies.Add(new WriteToDisk((uint) cacheReload, cacheLocation));
            }

            var sourceLc = configuration.GetSection("Datasource").GetDataSourceLc();
            var sourceOsm = configuration.GetSection("Datasource").GetDataSourceOsm();

            if (sourceOsm == null && sourceLc == null)
            {
                throw new ArgumentException(
                    "Use either a linked-connections or OSM-scheme as datasource, but not both at the same time");
            }


            if (sourceLc != null)
            {
                var source = sourceLc.Value;
                Log.Information($"Found LC data source {source.connections}, {source.locations}");
                if (reloadingPolicies.Any())
                {
                    (synchronizer, _) =
                        db.UseLinkedConnections(source.connections, source.locations, reloadingPolicies);
                }
                else
                {
                    Log.Warning(
                        "No reloading policies are found, so no fresh data will be loaded. Only an existing cache could be reused. If that failed too, the transitDB might be empty");
                }
            }

            if (sourceOsm != null)
            {
                Log.Information($"Found OSM data source {sourceOsm}");
                synchronizer = db.UseOsmRoute(sourceOsm, reloadingPolicies);
            }


            return (name, db, synchronizer);
        }

        private static string GetDataSourceOsm(this IConfiguration config)
        {
            return config.GetValue<string>("OsmRelation");
        }

        private static (string locations, string connections)? GetDataSourceLc(this IConfiguration config)
        {
            var locations = config.GetValue<string>("Locations");
            var cons = config.GetValue<string>("Connections");
            if (locations != null && cons != null)
            {
                return (locations, cons);
            }

            if (locations == null && cons == null)
            {
                return null;
            }

            throw new Exception(
                "Error in configuration: Datasource of transitDB is incorrect: both 'Location' and 'Connections' are needed to use Linked Connections, but only one of them is given");
        }

        /// <summary>
        /// Tries to load the transitDB from disk. Gives null if loading failed
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static TransitDb TryLoadFromDisk(string path, uint id)
        {
            if (!File.Exists(path))
            {
                Log.Information($"TransitDb {path} does not exist. Skipping load");
                return null;
            }

            try
            {
                Log.Information($"Attempting to read a transitDB from {path}");
                var db = TransitDb.ReadFrom(path, id);
                Log.Information("All read! Trying to determine loaded period");

                try
                {
                    // This is a bit of an unstable hack, only good for logging and debugging
                    var enumerator = db.Latest.ConnectionsDb.GetDepartureEnumerator();
                    enumerator.MoveNext(new DateTime(DateTime.Now.Year, 1, 1));
                    enumerator.MoveNext();
                    var startDate = enumerator.DepartureTime.FromUnixTime();

                    enumerator = db.Latest.ConnectionsDb.GetDepartureEnumerator();

                    enumerator.MoveNext(new DateTime(DateTime.Now.Year + 1, 1, 1));
                    enumerator.MovePrevious();
                    var endDate = enumerator.DepartureTime.FromUnixTime();


                    Log.Information(
                        $"Loaded transitdb from {path}. Estimated range {startDate} --> {endDate} ");
                }
                catch (Exception e)
                {
                    Log.Warning(e.Message);
                }


                return db;
            }
            catch (Exception e)
            {
                Log.Error(
                    $"Could not load transitDB from disk (path is {path}). Will start with an empty transitDB instead.\n{e.Message}");
                return null;
            }
        }


        private static List<ISynchronizationPolicy> GetSynchronizedWindows(this IConfiguration configuration)
        {
            var windows = new List<ISynchronizationPolicy>();

            foreach (var conf in configuration.GetChildren())
            {
                windows.Add(conf.GetSynchronizedWindow());
            }

            return windows;
        }

        private static SynchronizedWindow GetSynchronizedWindow(this IConfiguration c)
        {
            var timeBefore = c.GetValue<int>("TimeBefore");
            var timeAfter = c.GetValue<int>("TimeAfter");
            var freq = c.GetValue<int>("ReloadEvery");
            var retries = c.GetValue("Retries", 0);
            var update = c.GetValue<bool>("ForceUpdate");


            return new SynchronizedWindow(
                (uint) freq,
                TimeSpan.FromSeconds(timeBefore),
                TimeSpan.FromSeconds(timeAfter),
                (uint) retries,
                update
            );
        }
    }
}