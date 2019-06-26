using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;

namespace Itinero.Transit.API.Tests.Functional
{
    public class PerfTest
    {
        public static List<string> Sources = new List<string>
        {
            "testdata_2019-06-24_2019-06-31/nmbs.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/gent-P&R.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/brugge-shuttlebus.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/delijn-ant.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/delijn-lim.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/delijn-ovl.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/delijn-vlb.latest.transitdb",
            "testdata_2019-06-24_2019-06-31/delijn-wvl.latest.transitdb"
        };

        public const string Gent = "http://irail.be/stations/NMBS/008892007";
        public const string Brugge = "http://irail.be/stations/NMBS/008891009";
        public const string Poperinge = "http://irail.be/stations/NMBS/008896735";
        public const string Vielsalm = "http://irail.be/stations/NMBS/008845146";


        public void Information(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Run(List<string> sources)
        {
            Information("Running perftest...");
            var dict = LoadTransitDbs(sources);


            var st = new State(dict, new OtherModeBuilder());
            State.GlobalState = st;

            for (int i = 0; i < 25; i++)
            {
                Information($"{i}/25");
                RunTest();
            }
        }

        private void RunTest()
        {
            var start = DateTime.Now;
            var from = Brugge;
            var to = Gent;

            var departure = new DateTime(2019, 06, 25, 9, 00, 00, DateTimeKind.Utc);
            var arrival = new DateTime(2019, 06, 25, 12, 00, 00, DateTimeKind.Utc);

            var profile = JourneyBuilder.CreateProfile(
                @from, to,
                "https://openplanner.team/itinero-transit/walks/crowsflight&maxDistance=500",
                180,
                maxNumberOfTransfers: 10);
            var (journeys, queryStart, queryEnd) = profile.BuildJourneys(@from, to, departure, arrival);

            // ReSharper disable once PossibleMultipleEnumeration
            if (journeys == null || !journeys.Any())
            {
                throw new Exception("No journeys found...");
            }


            var end = DateTime.Now;
            Information($"Found {journeys.Count} journeys in {(end - start).TotalMilliseconds}ms");
        }

        private Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)> LoadTransitDbs(
            IEnumerable<string> sources)
        {
            var dict
                = new Dictionary<string, (TransitDb tdb, Synchronizer synchronizer)>();

            var i = (uint) 0;
            foreach (var source in sources)
            {
                var s = DateTime.Now;
                var tdb = TransitDb.ReadFrom(source, i);
                dict.Add(source, (tdb, null));
                i++;
                var e = DateTime.Now;
                Information($"Loaded tdb {source} in {(e - s).TotalMilliseconds}ms");
            }

            return dict;
        }
    }
}