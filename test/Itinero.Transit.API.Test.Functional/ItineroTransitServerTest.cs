using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.API.Tests.Functional
{
    public class ItineroTransitServerTest : ServerTest
    {
        private static readonly Dictionary<string, string> _knownUrls = new Dictionary<string, string>
        {
            {"localhost", "http://localhost:5000/"},
            {"dev", "http://localhost:5000/"},
            {"staging", "https://staging.anyways.eu/transitapi"},
            {"prod", "https://routing.anyways.eu/transitapi"},
            {"production", "https://routing.anyways.eu/transitapi"}
        };


        public ItineroTransitServerTest() : base(
            "Itinero-Transit Tester",
            "production",
            _knownUrls,
            defaultTimeout: 2000)
        {
        }

        public static string TestDepartureTime(TimeSpan addTime = default(TimeSpan))
        {
            var testMoment = DateTime.Now.Date.AddHours(10).Add(addTime);
            return testMoment.ToString("s");
        }


        private void DeLijnChallenge(
            string endpoint,
            string name,
            Dictionary<string, string> keyValues = null,
            Action<JToken> property = null,
            uint maxTimeAllowed = 0)
        {
            keyValues?.Add("test", "true");
            keyValues?.Add("operators", "DeLijn");


            Challenge(
                endpoint,
                name,
                keyValues,
                property,
                maxTimeAllowed);
        }


        protected override void RunTests()
        {
            Challenge("status", "Is the server online?",
                property: jobj =>
                {
                    try
                    {
                        AssertTrue(jobj["online"].Value<bool>(), "Not online");
                        var loadedWindows = jobj["loadedOperators"].Count();
                        AssertTrue(loadedWindows > 0, "Not all operators are loaded");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{e.Message} {e.StackTrace}");
                    }
                }
            );

            SncbTestSet();


            //  DeLijnTestSet();
        }

        private void DeLijnTestSet()
        {
            DeLijnChallenge("LocationsByName", "Search for station 'Nikolaas Gombertstraat'",
                new Dictionary<string, string>
                {
                    {"name", "N.Gombertstraat"}
                },
                jobj =>
                {
                    var stationBrugge = jobj[0];
                    AssertEqual("https://data.delijn.be/stops/507130",
                        stationBrugge["location"]["id"].Value<string>(),
                        "Search returned something else then the GombertstraatStop");
                    AssertEqual(0,
                        stationBrugge["difference"].Value<int>(),
                        "Exact search is wrong"
                    );
                }
            );

            DeLijnChallenge("Location", "Load information about stop Gombertstraat",
                new Dictionary<string, string>
                {
                    {"id", "https://data.delijn.be/stops/507130"},
                },
                jobj => { AssertEqual("https://data.delijn.be/stops/507130", jobj["id"]); }
            );

            DeLijnChallenge("Location/Connections", "Load connections of station Brugge",
                new Dictionary<string, string>
                {
                    {"id", "https://data.delijn.be/stops/507130"},
                    {"windowStart", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertEqual("https://data.delijn.be/stops/507130", jobj["location"]["id"]);
                    AssertTrue(jobj["segments"].Any(), "No segments found, exepcts at least some");
                }
            );
        }


        # region SNCB  

        private void SncbChallenge(
            string endpoint,
            string name,
            Dictionary<string, string> keyValues = null,
            Action<JToken> property = null,
            uint maxTimeAllowed = 0)
        {
            keyValues?.Add("test", "true");
            keyValues?.Add("operators", "nmbs");


            Challenge(
                endpoint,
                name,
                keyValues,
                property,
                maxTimeAllowed);
        }

        private void SncbTestSet()
        {
            SncbChallenge("LocationsByName", "Search for station 'Bruges'",
                new Dictionary<string, string>
                {
                    {"name", "Bruges"}
                },
                jobj =>
                {
                    var stationBrugge = jobj[0];
                    AssertEqual("http://irail.be/stations/NMBS/008891009",
                        stationBrugge["location"]["id"].Value<string>(),
                        "Search returned something else then the main station of Bruges");
                    AssertEqual(0,
                        stationBrugge["difference"].Value<int>(),
                        "Exact search is wrong"
                    );
                }
            );

            SncbChallenge("LocationsByName", "Search for station 'Brug'",
                new Dictionary<string, string>
                {
                    {"name", "Brug"}
                },
                jobj =>
                {
                    // The first entry should respond with NMBS-station 'Bruges'
                    var stations = (JArray) jobj;

                    var found = false;
                    foreach (var station in stations)
                    {
                        if (!station["location"]["id"].Value<string>()
                            .Equals("http://irail.be/stations/NMBS/008891009"))
                        {
                            continue;
                        }

                        found = true;
                        AssertEqual(2,
                            station["difference"].Value<int>(),
                            "Exact search is wrong"
                        );
                        break;
                    }

                    if (!found)
                    {
                        throw new Exception("Brugge was not found!");
                    }
                }
            );


            SncbChallenge("LocationsAround", "Search Locations around",
                new Dictionary<string, string>
                {
                    {"lat", "51.1978"},
                    {"lon", "3.2184"},
                    {"distance", "500"}
                },
                jobj =>
                {
                    var ids = jobj.Select(location => location["id"]).ToList();
                    //  AssertTrue(
                    //      ids.Contains("https://www.openstreetmap.org/node/6348496391"), "CentrumShuttle stop not found");
                    AssertTrue(
                        ids.Contains("http://irail.be/stations/NMBS/008891009"), "Station Brugge not found");
                }
            );


            SncbChallenge("Location", "Get info about station of Bruges",
                new Dictionary<string, string>
                {
                    {"id", "http://irail.be/stations/NMBS/008891009"}
                },
                jobj =>
                {
                    AssertEqual(5119723, (int) (100000 * jobj["lat"].Value<double>()));
                    AssertEqual(321673, (int) (100000 * jobj["lon"].Value<double>()));
                    AssertEqual("http://irail.be/stations/NMBS/008891009", jobj["id"].Value<string>());
                    AssertEqual("Bruges", jobj["name"].Value<string>());
                }
            );


            SncbChallenge("Location/Connections", "Load connections of station Brugge",
                new Dictionary<string, string>
                {
                    {"id", "http://irail.be/stations/NMBS/008891009"},
                    {"windowStart", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertEqual("http://irail.be/stations/NMBS/008891009", jobj["location"]["id"]);
                    AssertTrue(jobj["segments"].Any());
                }
            );


            SncbChallenge("Journey", "EAS, Brugge -> Ghent",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Count() > 0, "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("http://irail.be/stations/NMBS/008891009", j["departure"]["location"]["id"],
                            "Wrong departure stations");
                        AssertEqual("Bruges", j["departure"]["location"]["name"], "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "PCS, Brugge -> Gent",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Count() > 0, "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("http://irail.be/stations/NMBS/008891009", j["departure"]["location"]["id"],
                            "Wrong departure stations");
                        AssertEqual("Bruges", j["departure"]["location"]["name"], "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "EAS, Brugge (OSM) -> Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "PCS, Brugge (OSM) -> Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "EAS, Gent -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008892007"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["departure"]["location"]["id"],
                            "Wrong departure stations");
                    }
                }
            );

            SncbChallenge("Journey", "PCS, Gent -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008892007"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["departure"]["location"]["id"],
                            "Wrong departure stations");
                    }
                }
            );

            SncbChallenge("Journey", "EAS, Ghent (OSM) -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.035900/3.710800",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "PCS, Gent (OSM) -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"}, // Close to bruges station
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.035900/3.710800",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            SncbChallenge("Journey", "Walk [EAS], Brugge (OSM) to Brugge (NMBS)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.197640/3.218469"}, // Close to bruges
                    {"to", "http://irail.be/stations/NMBS/008891009"}, // station of bruges
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.197640/3.218469",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008891009", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                        AssertEqual(0, j["vehiclesTaken"].Value<int>());
                    }
                }
            );

            SncbChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween,  Pietervdvn Brugge -> Ghent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=5000&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.215770/3.218230",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                },
                maxTimeAllowed: 1500
            );
            SncbChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween,  pietervdvn Brugge -> Ghent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.215770/3.218230",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            SncbChallenge("Journey",
                "EAS with FirstLastMile walk, crow inbetween, Pietervdvn Brugge -> De Sterre, Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "https://www.openstreetmap.org/#map=14/51.0250/3.7129"}, // To Ghent: De Sterre
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=3000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=3000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");

                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.215770/3.218230",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.025000/3.712900",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            SncbChallenge("Journey",
                "PCS with FirstLastMile walk, crow inbetween, Pietervdvn Brugge -> De Sterre, Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "https://www.openstreetmap.org/#map=14/51.0250/3.7129"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=5000&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=50000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");

                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.215770/3.218230",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.025000/3.712900",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey",
                "EAS with FirstLastMile walk, crow inbetween, Pietervdvn Brugge -> Close to Ghent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "https://www.openstreetmap.org/#map=16/51.0374/3.7151"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.215770/3.218230",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.037400/3.715100",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey",
                "PCS with FirstLastMile walk, crow inbetween, Rijselsestraat Brugge -> Close to Ghent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "https://www.openstreetmap.org/#map=16/51.0374/3.7151"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.215770/3.218230",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.037400/3.715100",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween, Adinkerke -> Gouvy",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0858/2.6017"},
                    {"to", "https://www.openstreetmap.org/#map=14/50.1886/5.9543"}, // Gouvy
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.085800/2.601700",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.188600/5.954300",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween, Adinkerke -> Gouvy",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0858/2.6017"},
                    {"to", "https://www.openstreetmap.org/#map=14/50.1886/5.9543"}, // Gouvy
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.085800/2.601700",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.188600/5.954300",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            SncbChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween, Poperinge -> Brussel (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008896735"},
                    {"to", "https://www.openstreetmap.org/#map=11/50.8469/4.2249"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=1500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("http://irail.be/stations/NMBS/008896735",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.846900/4.224900",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween, Poperinge -> Brussel (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008896735"},
                    {"to", "https://www.openstreetmap.org/#map=14/50.8430/4.3279"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=1500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("http://irail.be/stations/NMBS/008896735",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.843000/4.327900",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            SncbChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween, Sint-Niklaas (OSM) -> BxlN (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.17236/4.14396"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.86044/4.35865"},

                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.172360/4.143960",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860440/4.358650",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            SncbChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween, Sint-Niklaas (OSM) -> BxlN (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.17236/4.14396"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.86044/4.35865"},
                    {"multipleOptions", "true"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.172360/4.143960",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860440/4.358650",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                },
                maxTimeAllowed: 4000
            );


            SncbChallenge("Journey", "EAS with FirstLastMile ebike, Sint-Niklaas (OSM) -> BxlN OSM",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.17236/4.14396"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.86044/4.35865"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.172360/4.143960",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860440/4.358650",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            SncbChallenge("Journey", "EAS with FirstLastMile ebike, Sint-Niklaas (OSM) -> BxlN OSM",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.17236/4.14396"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.86044/4.35865"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.172360/4.143960",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860440/4.358650",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            SncbChallenge("Journey", "EAS with FirstLastMile ebike, BxlN (OSM) -> Aalst (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/50.86051035579558/4.358399302117419"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.942586962931955/4.038028645195254"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860510/4.358399",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.942586/4.038028",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            SncbChallenge("Journey", "PCS with FirstLastMile ebike, BxlN (OSM) -> Aalst (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/50.86051035579558/4.358399302117419"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.942586962931955/4.038028645195254"},
                    {"multipleOptions", "true"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike")
                    }
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860510/4.358399",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.942586/4.038028",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            SncbChallenge("Journey", "PCS with FirstLastMile ebike, WTC3 BxlN (OSM) -> Gent (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/50.86094/4.35405"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.03465/3.70832"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "osm&maxDistance=5000&profile=ebike")
                    }
                },
                jobj =>
                {
                    AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null");
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    var i = 0;
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860940/4.354050",
                            j["departure"]["location"]["id"],
                            $"Wrong departure station in journey number {i}");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.034650/3.708320",
                            j["arrival"]["location"]["id"],
                            $"Wrong arrival station in journey number {i}");
                        i++;
                    }
                },
                maxTimeAllowed: 3000
            );

            SncbChallenge(
                "journey",
                "EAS (speedpedelec, walk, walk), Rijselsestraat Brugge (OSM) ->  Bxl Centr (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.1951297895458/3.214899786053735"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.84388967236137/4.354740038815095"},
                    {"departure", TestDepartureTime()},
                    {"inBetweenOsmProfile", "pedestrian"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "speedPedelec"},
                    {"firstMileSearchDistance", "50000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"}
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); }, 4000
            );
            SncbChallenge(
                "journey",
                "PCS (speedpedelec, walk, walk), Rijselsestraat Brugge (OSM) ->  Bxl Centr (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.1951297895458/3.214899786053735"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.84388967236137/4.354740038815095"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {"inBetweenOsmProfile", "pedestrian"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "speedPedelec"},
                    {"firstMileSearchDistance", "50000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"}
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); }, 4000
            );


            SncbChallenge("journey", "EAS (cycle, crow, walk), Beveren (OSM) -> St Niklaas (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.21523909670509/4.268520576417103"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.16509172615645/4.135866853010015"},
                    {"departure", TestDepartureTime()},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "10000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"},
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); }, maxTimeAllowed: 4000);

            SncbChallenge("journey", "PCS (cycle, crow, walk), Beveren (OSM) -> St Niklaas (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.21523909670509/4.268520576417103"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.16509172615645/4.135866853010015"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "10000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"},
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); }, maxTimeAllowed: 4000);


            SncbChallenge("Journey", "EAS (cycle, crow, walk), Antw Berchem (OSM) -> Brussel Warandepark (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.199993/4.431101"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.843183/4.371755"},
                    {"departure", TestDepartureTime()},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "10000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance ", "10000"},
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); },
                maxTimeAllowed: 5000
            );
            SncbChallenge("Journey", "PCS (cycle, crow, walk), Antw Berchem (OSM) -> Brussel Warandepark (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.199993/4.431101"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.843183/4.371755"},
                    {"departure", TestDepartureTime()},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "10000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance ", "10000"},
                    {"multipleOptions", "true"}
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); },
                maxTimeAllowed: 5000
            );

            SncbChallenge("journey",
                "Cycle [EAS] (cycle, crow, walk), Nieuwmunster -> Zuienkerke, expects cycling only",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=19/51.270256567260844/3.0617134555123755"
                    }, // Nieuwmunster
                    {"to", "https://www.openstreetmap.org/#map=16/51.2646/3.1546"}, // Zuienkerke
                    {"departure", TestDepartureTime()},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "0"}, {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "20000"}, {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"}
                },
                jobj =>
                {
                    AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual(0, j["vehiclesTaken"].Value<int>(),
                            "This journey should not take any PT-vehicle, only cycle");
                    }
                },
                maxTimeAllowed: 4000);

            SncbChallenge("Journey", "Direct walk, no PT with PCS (cycle, crow, walk), Heidelberg -> Heidelberg",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=18/49.42725/8.68574"},
                    {"to", "https://www.openstreetmap.org/#map=17/49.41836/8.67349"},
                    {"departure", TestDepartureTime()},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "10000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance ", "10000"},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertEqual(0, jobj["journeys"].ToList().Count);
                    AssertTrue(100 < jobj["directWalk"]["coordinates"].ToList().Count);
                },
                maxTimeAllowed: 5000
            );


            SncbChallenge("journey", "Graceful PCS fail: impossible journey should give valid (but empty) result",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"}, // Brugge
                    {"to", "http://irail.be/stations/NMBS/008892007"}, // Gent
                    {"departure", TestDepartureTime()},
                    {"arrival", TestDepartureTime(TimeSpan.FromMinutes(3))},
                    {"multipleOptions", "true"}
                },
                property: jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");
                    AssertNull(jobj["directWalk"].Value<string>(), "directwalk is not null");
                    AssertEqual(jobj["journeys"].Count(), 0, "Journeys are found?");
                });

            SncbChallenge("journey", "Graceful EAS fail: impossible journey should give valid (but empty) result",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"}, // Brugge
                    {"to", "http://irail.be/stations/NMBS/008892007"}, // Gent
                    {"departure", TestDepartureTime()},
                    {"arrival", TestDepartureTime(TimeSpan.FromMinutes(3))},
                    {"multipleOptions", "false"}
                },
                property: jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");
                    AssertNull(jobj["directWalk"].Value<string>(), "directwalk is not null");

                    AssertEqual(jobj["journeys"].Count(), 0, "Journeys are found?");
                });


            GenerateRandomSncbChallenge();
        }

        public Challenge GenerateRandomSncbChallenge(int range = 5000)
        {
            var random = new Random();

            double RandomLat()
            {
                var min = 49.5294835476;
                var max = 51.4750237087;
                return
                    (max - min) * random.NextDouble() + min;
            }

            double RandomLon()
            {
                var min = 2.51357303225;
                var max = 6.15665815596;
                return (max - min) * random.NextDouble() + min;
            }

            return Challenge("Journey",
                $"PCS journey between random locations, range is {range}",
                new Dictionary<string, string>
                {
                    {"test", "true"},
                    {"from", $"https://www.openstreetmap.org/#map=17/{RandomLat()}/{RandomLon()}"},
                    {"to", $"https://www.openstreetmap.org/#map=14/{RandomLat()}/{RandomLon()}"},
                    {"operators", "sncb"},
                    {"departure", TestDepartureTime()},
                    {
                        "walksGeneratorDescription",
                        "firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            $"osm&maxDistance={range}&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            $"osm&maxDistance={range}&profile=pedestrian")
                    }
                },
                jobj =>
                {
                    var found = jobj["journeys"].ToList().Count;
                    Console.WriteLine($"Found {found} journeys");
                },
                maxTimeAllowed: 120000,
                handleError: msg =>
                {
                    if (msg.Contains("Could not find a station that is"))
                    {
                        Console.WriteLine("  No stations in range");
                        return;
                    }

                    if (msg.Contains("Could not find a route towards/from the"))
                    {
                        Console.WriteLine("  No route to station");
                        return;
                    }

                    Console.WriteLine(msg);
                }
            );
        }

        #endregion
    }
}