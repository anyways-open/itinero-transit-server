using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using JsonLD.Core;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Provides the bindings to Itinero-transit.
    /// </summary>
    public class PublicTransportRouter
    {
        public static readonly PublicTransportRouter BelgiumSncb
            = new PublicTransportRouter(BelgiumSncbProfile());


        private readonly Profile<TransferStats> _profile;

        public PublicTransportRouter(Profile<TransferStats> profile)
        {
            _profile = profile;
        }

        public Uri AsLocationUri(string uriData)
        {
            var loc = new Uri(uriData);
            if (!_profile.LocationProvider.ContainsLocation(loc))
            {
                throw new KeyNotFoundException($"The specified location {uriData} is unknown or malformed.");
            }

            return loc;
        }

        public StationInfo GetLocationInfo(Uri uri)
        {
            return new StationInfo(_profile.GetCoordinateFor(uri));
        }
        
        
        public JourneyInfo EarliestArrivalRoute(Uri departureStation, Uri arrivalStation,
            DateTime departureTime, DateTime latestArrival)
        {
            var eas = new EarliestConnectionScan<TransferStats>(
                departureStation, arrivalStation, departureTime, latestArrival, _profile);

            var journey = eas.CalculateJourney();
            return JourneyInfo.FromJourney(this, 0, journey);
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

        private static Profile<TransferStats> BelgiumSncbProfile()
        {
            // The SNCB router expects a routerDB (based on OSM) at "./belgium.routerdb"
            // If that routerDB ain't there, we create it
            // TBH, we don't really need it... But meh
            //CreateRouterDb("http://files.itinero.tech/data/OSM/planet/europe/belgium-latest.osm.pbf",
            //  "belgium.routerdb");


            var sncbConnectionsSource = new Uri("http://graph.irail.be/sncb/connections");
            var cons = new LinkedConnectionProvider(sncbConnectionsSource,
                "http://graph.irail.be/sncb/connections{?departureTime}", new Downloader(caching : false));


            var locationSource = new Uri("http://irail.be/stations");
            var loc = new LocationsFragment(locationSource);
            loc.Download(new JsonLdProcessor(new Downloader(caching : false), locationSource));

            var p = new Profile<TransferStats>(
                cons,
                loc,
                new InternalTransferGenerator(180), 
                TransferStats.Factory,
                TransferStats.ProfileCompare,
                TransferStats.ParetoCompare);
            p.IntermodalStopSearchRadius = 0;
            p.EndpointSearchRadius = 0;
            return p;
        }
    }
}