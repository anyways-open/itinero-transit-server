using System;
using System.Collections.Generic;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Logic.Transfers;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Itinero.Transit.IO.OSM.Data;
using Itinero.Transit.Journey;
using Itinero.Transit.Journey.Metric;
using Itinero.Transit.OtherMode;
using Itinero.Transit.Utils;
using Xunit;
using Operator = Itinero.Transit.Api.Logic.Operator;

namespace Itinero.Transit.API.Tests
{
    public class JourneyTranslationTest
    {
        [Fact]
        public void TranslateJourney_SimpleJourney_CorrectTranslation()
        {
            var depDate = new DateTime(2019, 06, 19, 10, 00, 00).ToUniversalTime();
            var tdb = new TransitDb(0);

            var writer = tdb.GetWriter();
            var stop0 = writer.AddOrUpdateStop(new Stop("https://example.org/stop0", (0, 0)));
            var stop1 = writer.AddOrUpdateStop(new Stop("https://example.org/stop1", (1, 1)));
            var stop2 = writer.AddOrUpdateStop(new Stop("https://example.org/stop2", (2, 2)));

            var trip0 = writer.AddOrUpdateTrip(new Trip("https://example.org/trip0", new OperatorId(),
                new Dictionary<string, string>() {{"headsign", "Oostende"}}));

            var conn0 = writer.AddOrUpdateConnection(
                new Connection("https://example.org/conn1",
                stop0, stop1,  depDate.AddMinutes(-10).ToUnixTime(),
                10 * 60,
                0, trip0));
            writer.Close();

            var con = tdb.Latest.ConnectionsDb;
            var connection = con.Get(conn0);

            var genesis = new Journey<TransferMetric>(stop0,
                depDate.ToUnixTime(), TransferMetric.Factory,
                Journey<TransferMetric>.EarliestArrivalScanJourney);

            var journey0 = genesis.ChainForward(conn0, connection);

            var journey1 = journey0.ChainSpecial(
                Journey<TransferMetric>.OTHERMODE, depDate.AddMinutes(15).ToUnixTime(),
                stop2, new TripId(uint.MaxValue, uint.MaxValue));


            var state = new State(new List<Operator>
            {
                new Operator("test", tdb, null, 0, null, null)
            }, null, null, null);

            var cache = new CoordinatesCache(new CrowsFlightTransferGenerator(), false);

            var translated = state.Operators.GetFullView().Translate(journey1, cache);
            Assert.Equal("https://example.org/stop0", translated.Segments[0].Departure.Location.Id);
            Assert.Equal("https://example.org/stop1", translated.Segments[0].Arrival.Location.Id);
            Assert.Equal("https://example.org/trip0", translated.Segments[0].Vehicle);


            Assert.Equal("https://example.org/stop1", translated.Segments[1].Departure.Location.Id);
            Assert.Equal("https://example.org/stop2", translated.Segments[1].Arrival.Location.Id);
            Assert.Null(translated.Segments[1].Vehicle);
        }

        [Fact]
        public void Translate_Journey_CorrectTranslation()
        {
            const ulong time = 15761461220ul;
            const ulong previousTime = 15761454920ul;
            
           
            
            var tdb = new TransitDb(0);
            var wr = tdb.GetWriter();
            var l = wr.AddOrUpdateStop(new Stop("Some Station", (51.789, 4.123 )));

            var rootL = new OsmStopsDb(1).GetId("https://www.openstreetmap.org/#map=17/51.20445/3.22701");
            var connection = new Connection(
                "testConnection", rootL, l, previousTime, (ushort) (time - previousTime), 0,new TripId(0, 0));

            var connId = wr.AddOrUpdateConnection(connection);
            wr.Close();
            var genesis = new Journey<TransferMetric>(rootL, previousTime, TransferMetric.Factory);
            var j = genesis.ChainSpecial(connId, previousTime, l, new TripId(3, 3));

            var op = new Operator("Op", tdb, null, 500, new string[] { }, new string[] { });
            var operators = new OperatorSet(new List<Operator> {op});
            var translatedSegment =
                operators.TranslateWalkSegment(j, new CoordinatesCache(new DummyOtherMode(), false));
            Assert.Contains("openstreetmap.org", translatedSegment.Departure.Location.Id);
            Assert.DoesNotContain("openstreetmap.org", translatedSegment.Arrival.Location.Id);
        }
    }
}