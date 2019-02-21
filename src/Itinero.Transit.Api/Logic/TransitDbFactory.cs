using System;
using System.Collections.Generic;
using System.IO;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.IO.LC;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;
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


            var db = new TransitDb();
            if (!string.IsNullOrEmpty(cacheLocation))
            {
                try
                {
                    using (var stream = File.OpenRead(cacheLocation))
                    {
                        db = TransitDb.ReadFrom(stream);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(
                        $"Could not load transitDB from disk (path is {cacheLocation}). Will start with an empty transitDB instead.\n{e.Message}");
                }
            }


            // TODO Use multiple sources
            Log.Information($"Found data source {source.connections}, {source.locations}");
            var (synchronizer, lcProfile) =
                db.UseLinkedConnections(source.connections, source.locations, reloadingPolicies);

            if (cacheReload > 0 && cacheReload < long.MaxValue)
            {
                db.AddSyncPolicy(new WriteToDisk((uint) cacheReload, cacheLocation));
            }

            return (db, lcProfile, synchronizer);
        }

        private static (string locations, string connections) GetDataSource(this IConfiguration config)
        {
            return (
                config.GetValue<string>("Locations"),
                config.GetValue<string>("Connections")
            );
        }


        private static List<SynchronizationPolicy> GetSynchronizedWindows(this IConfiguration configuration)
        {
            var windows = new List<SynchronizationPolicy>();

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