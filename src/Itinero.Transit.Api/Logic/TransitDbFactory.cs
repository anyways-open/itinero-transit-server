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
            var perc = 1.0 * ((1 + batchCount) / nrOfBatches) * (1.0*currentCount / batchTarget);
            State.CurrentlyLoadingWindow = (State.CurrentlyLoadingWindow.Value.start,
                State.CurrentlyLoadingWindow.Value.end, perc);
        }


        public static (TransitDb, LinkedConnectionDataset) CreateTransitDb(
            string locationsUri, string connectionsUri)
        {
            // var sncb = new Profile(new Uri("https://graph.irail.be/sncb/connections"), new Uri("https://irail.be/stations"), new Downloader(false));


            var dataset = new LinkedConnectionDataset(new Uri(locationsUri), new Uri(connectionsUri), new Downloader());

            void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end)
            {
                Log.Information($"Loading time window {start}->{end}");
                State.CurrentlyLoadingWindow = (start, end, 0);
                dataset.AddAllConnectionsTo(w, start, end, Log.Warning, new LoggingOptions(OnConnectionLoaded, 1));
                State.CurrentlyLoadingWindow = null;
            }

            var db = new TransitDb(UpdateTimeFrame);

            var writer = db.GetWriter();
            dataset.AddAllLocationsTo(writer, Log.Error, new LoggingOptions(OnLocationLoaded, 250));
            writer.Close();


            return (db, dataset);
        }
    }
}