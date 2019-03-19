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
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static (TransitDb db, LinkedConnectionDataset lcProfile, Synchronizer synchronizer) CreateTransitDb(
            this IConfiguration configuration)
        {
            var source = configuration.GetSection("Datasource").GetDataSource();

            var reloadingPolicies =
                configuration.GetSection("ReloadPolicy")
                    .GetSection("Windows")
                    .GetSynchronizedWindows();

            var cacheLocation = configuration.GetValue<string>("Cache");
            var cacheReload = configuration.GetValue("CacheUpdateEvery", long.MaxValue);


            TransitDb db = null;
            if (!string.IsNullOrEmpty(cacheLocation))
            {
                db = TryLoadFromDisk(cacheLocation);
            }

            db = db ?? new TransitDb();


            Log.Information($"Found data source {source.connections}, {source.locations}");
            Synchronizer synchronizer = null;
            LinkedConnectionDataset lcProfile = null;
            
            reloadingPolicies.Add(new ImportanceCounter());

            if (cacheReload > 0 && cacheReload < long.MaxValue)
            {
                reloadingPolicies.Add(new WriteToDisk((uint) cacheReload, cacheLocation));
            }
            
            if (reloadingPolicies.Any())
            {
                (synchronizer, lcProfile) =
                    db.UseLinkedConnections(source.connections, source.locations, reloadingPolicies);
            }


            
            return (db, lcProfile, synchronizer);
        }

        /// <summary>
        /// Tries to load the transitDB from disk. Gives null if loading failed
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static TransitDb TryLoadFromDisk(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    Log.Information($"Attempting to read a transitDB from {path}");
                    var db = TransitDb.ReadFrom(stream);
                    Log.Information("All read! Trying to determine loaded period");

                    try
                    {
                        // This is a bit of an unstable hack, only good for logging and debugging
                        var enumerator = db.Latest.ConnectionsDb.GetDepartureEnumerator();
                        enumerator.MoveNext(new DateTime(DateTime.Now.Year, 1, 1));
                        enumerator.MoveNext();
                        var startDate = enumerator.DepartureTime.FromUnixTime();

                        enumerator = db.Latest.ConnectionsDb.GetDepartureEnumerator();

                        enumerator.MoveNext(new DateTime(DateTime.Now.Year+1, 1, 1));
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
            }
            catch (Exception e)
            {
                Log.Error(
                    $"Could not load transitDB from disk (path is {path}). Will start with an empty transitDB instead.\n{e.Message}");
                return null;
            }
        }

        private static (string locations, string connections) GetDataSource(this IConfiguration config)
        {
            return (
                config.GetValue<string>("Locations"),
                config.GetValue<string>("Connections")
            );
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