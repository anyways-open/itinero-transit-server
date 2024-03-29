using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Controllers;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Xunit;
using Operator = Itinero.Transit.Api.Logic.Operator;

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
            var stop0 = wr.AddOrUpdateStop(new Stop("abc", (1.0, 1.0), new Dictionary<string, string>
            {
                {"name", "abc"}
            }));
            var stop1 = wr.AddOrUpdateStop(new Stop("def", (1.0, 1.5), new Dictionary<string, string>
            {
                {"name", "def"}
            }));

            var tr0 = wr.AddOrUpdateTrip("tr0");
            var tr1 = wr.AddOrUpdateTrip("tr1");
            var tr2 = wr.AddOrUpdateTrip("tr2");
            
            // Too early
            wr.AddOrUpdateConnection(new Connection(stop0, stop1, "conn3",dt.AddMinutes(-15), 1000,  tr0, 0));
            // This one we are searching for
            wr.AddOrUpdateConnection(new Connection(stop0, stop1, "conn0", dt.AddMinutes(5), 1000, tr1, 0));

            // Wrong departure station
            wr.AddOrUpdateConnection(new Connection(stop1, stop0, "conn1", dt.AddMinutes(15), 1000, tr2, 0));

            // To late: falls out of the window of 1hr
            wr.AddOrUpdateConnection(new Connection(stop0, stop1, "conn2", dt.AddMinutes(65), 1000, tr0, 0));


            
            wr.Close();

            var transitDbs = new List<Operator>
            {
                new Operator("someOperator", tdb, null, 0, null, null)
            };
            State.GlobalState = new State(transitDbs, null, null, null);
            var lc = new LocationController();
      
            // Gets the connections for the given lcoation
            var result = lc.GetConnections("abc", dt);
            
            
            var returnedSegments = result.Value.Segments;
            Assert.NotNull(returnedSegments);
            Assert.Single(returnedSegments);
            Assert.Equal(dt.AddMinutes(5), returnedSegments[0].Departure.Time);
        }
    }
}