using System;
using System.Collections.Generic;
using Itinero.Transit.Algorithms.CSA;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using Itinero.Transit.IO.LC;
using Itinero.Transit.Journeys;
using Itinero.Transit.Logging;
using JsonLD.Core;
using Serilog;
using IConnection = Itinero.Transit.Data.IConnection;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Provides the bindings to Itinero-transit.
    /// </summary>
    internal class PublicTransportRouter
    {
        public static readonly PublicTransportRouter BelgiumSncb
            = BelgiumSncbProfile();

        private readonly StopsDb _stopsDb;
        private readonly ConnectionsDb _connectionsDb;
        private readonly Profile<TransferStats> _profile;
        
        public PublicTransportRouter(Profile<TransferStats> profile, StopsDb stopsDb, ConnectionsDb connectionsDb)
        {
            _profile = profile;
            _stopsDb = stopsDb;
            _connectionsDb = connectionsDb;
        }

//        public Uri AsLocationUri(string uriData)
//        {
//            var loc = new Uri(uriData);
//            if (!_locationDecoder.ContainsLocation(loc))
//            {
//                throw new KeyNotFoundException($"The specified location {uriData} is unknown or malformed.");
//            }
//
//            return loc;
//        }

        public StationInfo GetLocationInfo((uint tileId, uint localId) id)
        {
            var reader = _stopsDb.GetReader();
            if (!reader.MoveTo(id))
            {
                return null;
            }
            
            return new StationInfo(reader);
        }


//        public StationInfo GetLocationInfo(Uri uri)
//        {
//            return new StationInfo(_locationDecoder.GetCoordinateFor(uri));
//        }

//        public Location GetCoord(ulong id)
//        {
//            return _locationDecoder.GetCoordinateFor(_reverseIds[id]);
//        }


        public IrailResponse<TransferStats> EarliestArrivalRoute(Uri departureStation, Uri arrivalStation,
            DateTime departureTime, DateTime latestArrival)
        {
            var reader = _stopsDb.GetReader();
            
            // get departure and arrival station ids.
            if (!reader.MoveTo(departureStation.ToString()))
            {
                return null;
            }
            var departureStationId = reader.Id;
            if (!reader.MoveTo(arrivalStation.ToString()))
            {
                return null;
            }
            var arrivalStationId = reader.Id;
            
            // run EAS.
            var eas = new EarliestConnectionScan<TransferStats>(
                departureStationId, arrivalStationId,
                departureTime.ToUnixTime(), departureTime.AddHours(10).ToUnixTime(),
                _profile);

            var journey = eas.CalculateJourney();
            return IrailResponse<TransferStats>.CreateResponse(this, journey);
        }

//        private static void CreateRouterDb(string downloadSource, string targetLocation, bool forceRefresh = false)
//        {
//            if (!File.Exists(targetLocation) || forceRefresh)
//            {
//                Log.Information($"RouterDB {targetLocation} missing, creating one...");
//                var itineroDownloadsBe =
//                    new Uri(downloadSource);
//
//                var fileReq = (HttpWebRequest) WebRequest.Create(itineroDownloadsBe);
//                var fileResp = (HttpWebResponse) fileReq.GetResponse();
//                using (var httpStream = fileResp.GetResponseStream())
//                {
//                    using (var fileStream = File.Create($"{targetLocation}.osm.pbf"))
//                    {
//                        // ReSharper disable once PossibleNullReferenceException
//                        httpStream.CopyTo(fileStream);
//                        fileStream.Close();
//                    }
//                }
//
//                using (var stream = File.OpenRead($"{targetLocation}.osm.pbf"))
//                {
//                    var routerDb = new RouterDb();
//                    Log.Information("Stream successfully opened...");
//                    routerDb.LoadOsmData(stream, Vehicle.Pedestrian);
//                    Log.Information("Serializing...");
//                    using (var outStream = new FileInfo(targetLocation).Open(FileMode.Create))
//                    {
//                        routerDb.Serialize(outStream);
//                    }
//                }
//
//                File.Delete($"{targetLocation}.osm.pbf");
//                Log.Information("DONE!");
//            }
//        }

        private static PublicTransportRouter BelgiumSncbProfile()
        {
            // The SNCB router expects a routerDB (based on OSM) at "./belgium.routerdb"
            // If that routerDB ain't there, we create it
            // TBH, we don't really need it... But meh
            //CreateRouterDb("http://files.itinero.tech/data/OSM/planet/europe/belgium-latest.osm.pbf",
            //  "belgium.routerdb");

            var profile = Itinero.Transit.IO.LC.CSA.Belgium.Sncb(new IO.LC.CSA.Utils.LocalStorage("cache"));

            var stopsDb = new StopsDb();
            var connectionsDb = new ConnectionsDb();

            connectionsDb.LoadConnections(profile, stopsDb, (DateTime.Now.Date, TimeSpan.FromDays(1)));
            
            var p = new Profile<TransferStats>(
                connectionsDb, stopsDb,
                new NoWalksGenerator(), new TransferStats());
            return new PublicTransportRouter(
                p,
                stopsDb,
                connectionsDb);
        }

//        public IConnection GetConnection(uint id)
//        {
//            _connReader.MoveTo(id);
//            return _connReader;
//        }

//        public Uri GetConnectionUri(uint id)
//        {
//            return _idToConnUri[id];
//        }
    }
}