using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Logic.Transfers;
using Itinero.Transit.Data.Core;
using Itinero.Transit.OtherMode;
using Xunit;

namespace Itinero.Transit.API.Tests
{
    public class OtherModeBuilderTest
    {
        [Fact]
        public void TestOtherModeBuilderSimple()
        {
            var omb = new OtherModeBuilder();

            var empty = new List<Stop>();

            var crow = omb.Create("crowsflight&speed=1.4&maxDistance=500", empty, empty);
            
            var cf = crow as CrowsFlightTransferGenerator;
            Assert.NotNull(cf);
            Assert.Equal((uint)500, cf.Range());
            var crow0 = omb.Create("crowsflight&speed=1&maxDistance=1500", empty, empty);
            var cf0 = crow0 as CrowsFlightTransferGenerator;
            Assert.NotNull(cf0);
            Assert.Equal((uint) 1500, cf0.Range());
        }

        [Fact]
        public void TestOtherModeBuilderFirstLast()
        {
            var omb = new OtherModeBuilder();

            var empty = new List<Stop>();


            var desc = "firstLastMile" +
                       "&firstMile=" + Uri.EscapeDataString(
                           "osm&profile=pedestrian&maxDistance=1000") +
                       "&default=" + Uri.EscapeDataString("crowsflight&maxDistance=1500") +
                       "&lastMile=" + Uri.EscapeDataString("osm&profile=pedestrian&maxDistance=5000");

            var gen = omb.Create(desc, empty, empty);
            var flm = gen as FirstLastMilePolicy;
            Assert.NotNull(flm);
            Assert.Equal((uint) 5000, flm.Range());


            var exp = "firstLastMile" +
                      "&default=crowsflight&maxDistance=1500&speed=1.4" +
                      "&firstMile=osm&maxDistance=1000&profile=pedestrian" +
                      "&lastMile=osm&maxDistance=5000&profile=pedestrian";
            Assert.Equal(exp, flm.OtherModeIdentifier());

            // Oops, we forgot to escape!
            desc = "firstLastMile" +
                   "&firstMile=" +
                   "osm&profile=pedestrian&maxDistance=1001" +
                   "&default=" + "crowsflight" +
                   "&lastMile=" + "osm";

            try
            {
                omb.Create(desc, empty, empty);
                Assert.True(false);
            }
            catch (ArgumentException)
            {
                // Expected behaviour       
            }
        }
    }
}