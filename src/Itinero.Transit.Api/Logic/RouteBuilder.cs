using System.Collections.Generic;
using Itinero.Transit.Api.Models;
using Itinero.Transit.IO.OSM;
using Serilog;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Builds a route (list of geometries) between two given points via the OSM network
    /// </summary>
    public static class RouteBuilder
    {
        public static List<Coordinate> Get(
            float fromLat,
            float fromLon,
            float toLat,
            float toLon,
            string profileName = "pedestrian",
            uint maxSearch = 2500
        )
        {
            var result = new List<Coordinate>();
            var profile = State.GlobalState.OtherModeBuilder.GetOsmProfile(profileName);
            // Note that the routable tile cache should already be set up
            var gen = new OsmTransferGenerator(maxSearch, profile);

            var route = gen.CreateRoute((fromLat, fromLon), (toLat, toLon), out var isEmpty);
            if (isEmpty || route == null)
            {
                Log.Warning($"No route found from {(fromLat, fromLon)} to {(toLat, toLon)} with profile {profileName} and maxdistance {maxSearch}m");
                return result;
            }

            

            foreach (var point in route.Shape)
            {
                result.Add(new Coordinate(point.Latitude, point.Longitude));
            }

            return result;
        }
    }
}