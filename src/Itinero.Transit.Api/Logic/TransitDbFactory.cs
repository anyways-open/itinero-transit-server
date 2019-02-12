using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.Utils;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    public class TransitDbFactory
    {
        private static void OnLocationLoaded((int, int, int, int) status)
        {
            var (currentCount, batchTarget, batchCount, nrOfBatches) = status;
            Log.Information(
                $"Importing locations: Running batch {batchCount}/{nrOfBatches}: Importing location {currentCount}/{batchTarget}");
        }


        private static void OnConnectionLoaded((int, int, int, int) status)
        {
            var (currentCount, batchTarget, batchCount, nrOfBatches) = status;
            Log.Information(
                $"Importing connections: Running batch {batchCount}/{nrOfBatches}: Importing timetable {currentCount} (out of an estimated {batchTarget})");
        }


        public static (TransitDb, LinkedConnectionDataset) CreateTransitDb(
            string locationsUri, string connectionsUri)
        {
            // var sncb = new Profile(new Uri("https://graph.irail.be/sncb/connections"), new Uri("https://irail.be/stations"), new Downloader(false));


            var dataset = new LinkedConnectionDataset(new Uri(locationsUri), new Uri(connectionsUri), new Downloader());

            void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end)
            {
                Log.Information($"Loading time window {start}->{end}");
                dataset.AddAllConnectionsTo(w, start, end, Log.Warning, new LoggingOptions(OnConnectionLoaded, 1));
            }

            var db = new TransitDb(UpdateTimeFrame);

            var writer = db.GetWriter();
            dataset.AddAllLocationsTo(writer, Log.Error, new LoggingOptions(OnLocationLoaded, 250));
            writer.Close();


            return (db, dataset);
        }
    }
}