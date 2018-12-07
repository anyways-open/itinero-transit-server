using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Itinero.IO.LC;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Walks;
using JsonLD.Core;
using OsmSharp.IO.PBF;
using Serilog;
using IConnection = Itinero.Transit.Data.IConnection;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Provides the bindings to Itinero-transit.
    /// </summary>
    public class PublicTransportRouter
    {
        public static readonly PublicTransportRouter BelgiumSncb
            = BelgiumSncbProfile();


        private readonly Data.Profile<TransferStats> _profile;
        private readonly ILocationProvider _locationDecoder;
        private readonly ConnectionsDb.ConnectionsDbReader _connReader;
        private readonly Dictionary<string, ulong> _locationIds;
        private readonly Dictionary<ulong, Uri> _reverseIds;
        private readonly Dictionary<ulong, Uri> _idToConnUri;

        public PublicTransportRouter(Data.Profile<TransferStats> profile, ILocationProvider locationDecoder,
            Dictionary<string, ulong> locationIds, Dictionary<ulong, Uri> reverseIds,
            Dictionary<ulong, Uri> idToConnUri)
        {
            _profile = profile;
            _locationDecoder = locationDecoder;
            _locationIds = locationIds;
            _reverseIds = reverseIds;
            _idToConnUri = idToConnUri;
            _connReader = profile.ConnectionsDb.GetReader();
        }

        public Uri AsLocationUri(string uriData)
        {
            var loc = new Uri(uriData);
            if (!_locationDecoder.ContainsLocation(loc))
            {
                throw new KeyNotFoundException($"The specified location {uriData} is unknown or malformed.");
            }

            return loc;
        }

        public StationInfo GetLocationInfo(ulong id)
        {
            return GetLocationInfo(_reverseIds[id]);
        }


        public StationInfo GetLocationInfo(Uri uri)
        {
            return new StationInfo(_locationDecoder.GetCoordinateFor(uri));
        }

        public Location GetCoord(ulong id)
        {
            return _locationDecoder.GetCoordinateFor(_reverseIds[id]);
        }


        public IrailResponse<TransferStats> EarliestArrivalRoute(Uri departureStation, Uri arrivalStation,
            DateTime departureTime, DateTime latestArrival)
        {
            var from = _locationIds[departureStation.ToString()];
            var to = _locationIds[arrivalStation.ToString()];

            var eas = new EarliestConnectionScan<TransferStats>(
                from, to,
                (ulong) departureTime.ToUnixTime(), (ulong) departureTime.ToUnixTime(),
                _profile);

            var journey = eas.CalculateJourney();
            return IrailResponse<TransferStats>.CreateResponse(this, journey);
        }

        private static void CreateRouterDb(string downloadSource, string targetLocation, bool forceRefresh = false)
        {
            if (!File.Exists(targetLocation) || forceRefresh)
            {
                Log.Information($"RouterDB {targetLocation} missing, creating one...");
                var itineroDownloadsBe =
                    new Uri(downloadSource);

                var fileReq = (HttpWebRequest) WebRequest.Create(itineroDownloadsBe);
                var fileResp = (HttpWebResponse) fileReq.GetResponse();
                using (var httpStream = fileResp.GetResponseStream())
                {
                    using (var fileStream = File.Create($"{targetLocation}.osm.pbf"))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        httpStream.CopyTo(fileStream);
                        fileStream.Close();
                    }
                }

                using (var stream = File.OpenRead($"{targetLocation}.osm.pbf"))
                {
                    var routerDb = new RouterDb();
                    Log.Information("Stream successfully opened...");
                    routerDb.LoadOsmData(stream, Vehicle.Pedestrian);
                    Log.Information("Serializing...");
                    using (var outStream = new FileInfo(targetLocation).Open(FileMode.Create))
                    {
                        routerDb.Serialize(outStream);
                    }
                }

                File.Delete($"{targetLocation}.osm.pbf");
                Log.Information("DONE!");
            }
        }

        private static PublicTransportRouter BelgiumSncbProfile()
        {
            // The SNCB router expects a routerDB (based on OSM) at "./belgium.routerdb"
            // If that routerDB ain't there, we create it
            // TBH, we don't really need it... But meh
            //CreateRouterDb("http://files.itinero.tech/data/OSM/planet/europe/belgium-latest.osm.pbf",
            //  "belgium.routerdb");




            var locationSource = new Uri("http://irail.be/stations");
            var loc = new LocationsFragment(locationSource);
            // This crashes with caching...
            loc.Download(new JsonLdProcessor(new Downloader(caching: false), locationSource));

            var sncbConnectionsSource = new Uri("http://graph.irail.be/sncb/connections");
            var cons = new LinkedConnectionProvider(sncbConnectionsSource,
                "http://graph.irail.be/sncb/connections{?departureTime}", new Downloader(caching: true));

            var oldProfile = new IO.LC.Profile<IO.LC.TransferStats>(
                cons, loc, null, IO.LC.TransferStats.Factory, null, null
            );


            var consDb = new ConnectionsDb();
            var stopsDb = new StopsDb();

            var connectionsDb = new ConnectionsDb();
            var stopIds = new Dictionary<string, ulong>();
            var reverseIds = new Dictionary<ulong, Uri>();
            foreach (var location in loc.GetAllLocations())
            {
                var v = stopsDb.Add(loc.Uri.ToString(), location.Lon, location.Lat);
                var id = (ulong) v.localTileId * uint.MaxValue + v.localId;

                var uri = location.Uri;
                stopIds[uri.ToString()] = id;
                reverseIds[id] = uri;
                Log.Information($"Location {id} is {uri}");
            }


            var dayToLoad = DateTime.Now.Date.AddHours(10);
            var idToConnUri = connectionsDb.LoadConnections(oldProfile, stopsDb,
                (dayToLoad, new TimeSpan(0, 2, 0, 0)), out _);


            var newProfile = new Data.Profile<TransferStats>(
                consDb,
                stopsDb,
                new NoWalksGenerator(),
                new TransferStats()
            );

            return new PublicTransportRouter(
                newProfile,
                loc,
                stopIds,
                reverseIds,
                idToConnUri);
        }

        public IConnection GetConnection(uint id)
        {
            _connReader.MoveTo(id);
            return _connReader;
        }

        public Uri GetConnectionUri(uint id)
        {
            return _idToConnUri[id];
        }
    }
}