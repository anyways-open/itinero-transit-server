using Itinero;
using Itinero.IO.Osm.Tiles;
using Itinero.Transit.Api;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.IO.OSM;
using Itinero.Transit.OtherMode;
using Xunit;

namespace Test
{
    public class RoutingTest
    {
        [Fact]
        public void TestRegressingRoute()
        {
            Startup.ConfigureLogging();
            var omb = new OtherModeBuilder("rt-cache");
            var cacher = (OtherModeCacher)
                omb.Create("osm&profile=pedestrian&maxDistance=5000", null, null);
            var osm = (OsmTransferGenerator) cacher.Fallback;
            var route = osm.CreateRoute(
                (50.865205, 4.35096799999999), // Herman teirlinck
                (50.86034, 4.36170), // Brussel Noord...
                out var isEmpty, out var errMssg);
            Assert.NotNull(route);
            Assert.False(isEmpty);
        }
    }
}