using System;
using Itinero.Transit.Data;
using Itinero.Transit.IO.LC;
using Itinero.Transit.IO.LC.CSA;
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


        public static (TransitDb, Profile) CreateTransitDbBelgium()
        {
            var sncb = Belgium.Sncb();


            void UpdateTimeFrame(TransitDb.TransitDbWriter w, DateTime start, DateTime end)
            {
                sncb.AddAllConnectionsTo(w, start, end, Log.Warning, new LoggingOptions(OnConnectionLoaded));
            }

            var db = new TransitDb(UpdateTimeFrame);

            var writer = db.GetWriter();
            sncb.AddAllLocationsTo(writer, Log.Error, new LoggingOptions(OnLocationLoaded, 250));
            writer.Close();
            
            return (db, sncb);
        }
    }
}