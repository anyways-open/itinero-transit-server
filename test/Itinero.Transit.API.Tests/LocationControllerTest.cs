using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Controllers;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Synchronization;
using Xunit;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.API.Tests
{
    public class LocationControllerTest
    {
        [Fact]
        public void GetConnections_SimpleTdb_ReturnsOneConnection()
        {
            var dt = new DateTime(2019, 09, 07, 10, 00, 00).ToUniversalTime();
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var stop0 = wr.AddOrUpdateStop("abc", 1.0, 1.0, new List<Attribute>
            {
                new Attribute("name", "abc")
            });
            var stop1 = wr.AddOrUpdateStop("def", 1.5, 1.0, new List<Attribute>
            {
                new Attribute("name", "def")
            });
            // This one we are searching for
            wr.AddOrUpdateConnection(stop0, stop1, "conn0", dt.AddMinutes(5), 1000, 0, 0, new TripId(0, 0), 0);

            // Wrong departure station
            wr.AddOrUpdateConnection(stop1, stop0, "conn1", dt.AddMinutes(15), 1000, 0, 0, new TripId(0, 0), 0);

            // To late: falls out of the window of 1hr
            wr.AddOrUpdateConnection(stop0, stop1, "conn2", dt.AddMinutes(65), 1000, 0, 0, new TripId(0, 0), 0);

            wr.Close();

            var transitDbs = new Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)>
            {
                {"x", (tdb, null)}
            };
            State.GlobalState = new State(transitDbs, null, null);
            var lc = new LocationController();
            var result = lc.GetConnections("abc", dt);
            var returnedSegments = result.Value.Segments;
            Assert.NotNull(returnedSegments);
            Assert.Single(returnedSegments);
            Assert.Equal(dt.AddMinutes(5), returnedSegments[0].Departure.Time);
        }
    }
}