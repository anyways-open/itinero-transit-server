using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.OSM.Data;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    public static class TransitDbFactory
    {
        public static Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> CreateTransitDbs(this IConfiguration configuration, bool dryRun = false)
        {
            // First, we read the reusable reload windows
            var reloadingPolicies = configuration.GetSection("ReloadingPolicies");
            var policies = new Dictionary<string, List<ISynchronizationPolicy>>();
            foreach (var rp in reloadingPolicies.GetChildren())
            {
                
                policies.Add(rp.GetValue<string>("Name"), rp.GetSection("Windows").GetSynchronizedWindows());
            }

            return configuration.GetSection("TransitDb").CreateTransitDbsFromConfig(policies, dryRun);
        }


        /// <summary>
        /// Builds the entire transitDB based on the relevant configuration section
        /// </summary>
        private static Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> CreateTransitDbsFromConfig(
            this IConfiguration configuration, Dictionary<string, List<ISynchronizationPolicy>> reusablePolicies,
            bool dryRun = false)
        {

            if (!configuration.GetChildren().Any())
            {
                throw new ArgumentException("The 'transitDb'-element has no children, no transitdbs defined");
            }
            
            var dbs = new Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)>();
            uint id = 0;
            foreach (var config in configuration.GetChildren())
            {
                try
                {
                    var (name, db, sync) = config.CreateTransitDb(id, reusablePolicies, dryRun);
                    dbs.Add(name, (db, sync));
                    id++;
                }
                catch (Exception e)
                {
                    Log.Error($"Could not load database with config {config}\n" +
                              $"Probable cause: upstream server is offline (or internet is down)\n" +
                              $"{e}");
                }
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (dryRun)
            {
                return null;
            }
            return dbs;
        }

        private static List<ISynchronizationPolicy> GetReloadPolicy(this IConfiguration rp,
            Dictionary<string, List<ISynchronizationPolicy>> reusable)
        {
            if (rp.GetSection("Windows").Value != null)
            {
                return rp.GetSection("Windows")
                    .GetSynchronizedWindows();
            }

            var nm = rp.Get<string>();
            // ReSharper disable once InvertIf
            if (nm != null)
            {
                if (!reusable.ContainsKey(nm))
                {
                    throw new KeyNotFoundException($"No reusable policy with name {nm} found");
                }

                return reusable[nm].ToList(); // Make a copy
            }

            throw new ArgumentException("No 'windows' or 'name' found in the reloadingPolicies");
        }

        /// <summary>
        /// Builds the entire transitDB based on the relevant configuration section
        /// </summary>
        private static (String name, TransitDb db, Synchronizer synchronizer) CreateTransitDb(
            this IConfiguration configuration, uint id,
            Dictionary<string, List<ISynchronizationPolicy>> reusablePolicies, bool dryRun = false)
        {
            var name = configuration.GetValue<string>("Name");


            
            var reloadingPolicies =
                configuration.GetSection("ReloadPolicy").GetReloadPolicy(reusablePolicies);

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
                var (locations, connections) = sourceLc.Value;
                Log.Information($"Found LC data source {connections}, {locations}");
                if (reloadingPolicies.Any())
                {
                    (synchronizer, _) =
                        db.UseLinkedConnections(connections, locations, reloadingPolicies);
                }
                else
                {
                    Log.Warning(
                        "No reloading policies are found, so no fresh data will be loaded. Only an existing cache could be reused. If that failed too, the transitDB might be empty");
                }
            }

            // ReSharper disable once InvertIf
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