using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.Utils;
using Xunit;
using Attribute = Itinero.Transit.Data.Attributes.Attribute;

namespace Itinero.Transit.API.Tests
{
    public class JourneyTranslationTest
    {
        [Fact]
        public void TestTranslation()
        {
            var depDate = new DateTime(2019, 06, 19, 10, 00, 00).ToUniversalTime();
            var tdb = new TransitDb(0);

            var writer = tdb.GetWriter();
            var stop0 = writer.AddOrUpdateStop("https://example.org/stop0", 0, 0);
            var stop1 = writer.AddOrUpdateStop("https://example.org/stop1", 1, 1);
            var stop2 = writer.AddOrUpdateStop("https://example.org/stop2", 2, 2);

            var trip0 = writer.AddOrUpdateTrip("https://example.org/trip0",
                new[] {new Attribute("headsign", "Oostende")});

            var conn0 = writer.AddOrUpdateConnection(stop0, stop1, "https://example.org/conn1", depDate.AddMinutes(-10),
                10 * 60,
                0, 0, trip0, 0);
            writer.Close();

            var con = tdb.Latest.ConnectionsDb;
           var connection = con.Get(conn0);

            var genesis = new Journey<TransferMetric>(stop0,
                depDate.ToUnixTime(), TransferMetric.Factory,
                Journey<TransferMetric>.EarliestArrivalScanJourney);

            var journey0 = genesis.ChainForward(connection);

            var journey1 = journey0.ChainSpecial(
                Journey<TransferMetric>.OTHERMODE, depDate.AddMinutes(15).ToUnixTime(),
                stop2, new TripId(uint.MaxValue, uint.MaxValue));


            var state = new State(new Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)>()
            {
                {"test", (tdb, null)}
            }, null, null, null);

            var translated = state.Translate(journey1, null);
            Assert.Equal("https://example.org/stop0", translated.Segments[0].Departure.Location.Id);
            Assert.Equal("https://example.org/stop1", translated.Segments[0].Arrival.Location.Id);
            Assert.Equal("https://example.org/trip0", translated.Segments[0].Vehicle);


            Assert.Equal("https://example.org/stop1", translated.Segments[1].Departure.Location.Id);
            Assert.Equal("https://example.org/stop2", translated.Segments[1].Arrival.Location.Id);
            Assert.Null(translated.Segments[1].Vehicle);
            
            
            
            
            
        }
    }
}