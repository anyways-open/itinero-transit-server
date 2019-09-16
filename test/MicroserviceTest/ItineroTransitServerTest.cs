using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MicroserviceTest
{
    public class ItineroTransitServerTest : ServerTest
    {
        private static readonly Dictionary<string, string> knownUrls = new Dictionary<string, string>
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
            knownUrls,
            defaultTimeout: 2000)
        {
        }

        private string TestDepartureTime()
        {
            var testMoment = DateTime.Now.Date.AddDays(1).AddHours(10);
            return testMoment.ToString("s");
        }

        /// <summary>
        /// Same as 'challenge', but adds 'test=true' parameter
        /// </summary>
        private void TestChallenge(
            string endpoint,
            string name,
            Dictionary<string, string> keyValues = null,
            Action<JToken> property = null,
            uint maxTimeAllowed = 0)
        {
            keyValues?.Add("test", "true");
            Challenge(
                endpoint,
                name,
                keyValues,
                property,
                maxTimeAllowed);
        }

        protected override void RunTests()
        {
            TestChallenge("status", "Is the server online?",
                property: jobj =>
                {
                    AssertTrue(jobj["online"].Value<bool>(), "Not online");
                    var loadedWindows = jobj["loadedTimeWindows"].Count();
                    AssertTrue(loadedWindows > 0, "Not all operators are loaded");
                }
            );

            TestChallenge("LocationsByName", "Search for station 'Brugge'",
                new Dictionary<string, string>
                {
                    {"name", "Brugge"}
                },
                jobj =>
                {
                    // The first entry should respond with NMBS-station 'Brugge'
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

            TestChallenge("LocationsByName", "Search for station 'Brugg'",
                new Dictionary<string, string>
                {
                    {"name", "Brugg"}
                },
                jobj =>
                {
                    // The first entry should respond with NMBS-station 'Brugge'
                    var stationBrugge = jobj[0];
                    AssertEqual("http://irail.be/stations/NMBS/008891009",
                        stationBrugge["location"]["id"].Value<string>(),
                        "Search returned something else then the main station of Bruges");
                    AssertEqual(1,
                        stationBrugge["difference"].Value<int>(),
                        "Exact search is wrong"
                    );
                }
            );


            TestChallenge("LocationsAround", "Search Locations around",
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


            TestChallenge("Location", "Get info about station of Bruges",
                new Dictionary<string, string>
                {
                    {"id", "http://irail.be/stations/NMBS/008891009"}
                },
                jobj =>
                {
                    AssertEqual("51.1972295551607", jobj["lat"].Value<string>());
                    AssertEqual(3.216724991798401, jobj["lon"].Value<double>());
                    AssertEqual("http://irail.be/stations/NMBS/008891009", jobj["id"].Value<string>());
                    AssertEqual("Brugge", jobj["name"].Value<string>());
                    AssertEqual("Bruges", jobj["translatedNames"]["fr"].Value<string>());
                }
            );


            TestChallenge("Location/Connections", "Load connections of station Brugge",
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


            TestChallenge("Journey", "EAS, Brugge -> Ghent",
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
                        AssertEqual("Brugge", j["departure"]["location"]["name"], "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS, Brugge -> Gent",
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
                        AssertEqual("Brugge", j["departure"]["location"]["name"], "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "EAS, Brugge (OSM) -> Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS, Brugge (OSM) -> Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "EAS, Gent -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008892007"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["departure"]["location"]["id"],
                            "Wrong departure stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS, Gent -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008892007"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["departure"]["location"]["id"],
                            "Wrong departure stations");
                    }
                }
            );

            TestChallenge("Journey", "EAS, Ghent (OSM) -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.0359/3.71080000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS, Gent (OSM) -> Brugge (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges station
                    {"departure", TestDepartureTime()},
                    {"multipleOptions", "true"}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.0359/3.71080000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            TestChallenge("Journey", "Walk [EAS], Brugge (OSM) to Brugge (NMBS)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges
                    {"to", "http://irail.be/stations/NMBS/008891009"}, // station of bruges
                    {"departure", TestDepartureTime()}
                },
                jobj =>
                {
                    AssertNotNull(jobj["journeys"], "Journeys are null");

                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.19764/3.21847",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008891009", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                        AssertEqual(0, j["vehiclesTaken"].Value<int>());
                    }
                }
            );

            TestChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween,  Rijselsestraat Brugge -> Ghent",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                },
                maxTimeAllowed: 1500
            );
            TestChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween,  Rijselsestraat Brugge -> Ghent",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            TestChallenge("Journey",
                "EAS with FirstLastMile walk, crow inbetween, Rijselsestraat Brugge -> De Sterre, Gent",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.025/3.71289999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            TestChallenge("Journey",
                "PCS with FirstLastMile walk, crow inbetween, Rijselsestraat Brugge -> De Sterre, Gent",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"},
                    {"to", "https://www.openstreetmap.org/#map=14/51.0250/3.7129"}, // To Ghent: De Sterre
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.025/3.71289999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey",
                "EAS with FirstLastMile walk, crow inbetween, Rijselsestraat Brugge -> Close to Ghent",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.0374/3.71510000000001",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.0374/3.71510000000001",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween, Adinkerke -> Gouvy",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.0858/2.60169999999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.1886/5.95429999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween, Adinkerke -> Gouvy",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.0858/2.60169999999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.1886/5.95429999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            TestChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween, Poperinge -> Brussel (OSM)",
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

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.8469/4.22489999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween, Poperinge -> Brussel (OSM)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008896735"},
                    {"to", "https://www.openstreetmap.org/#map=11/50.8469/4.2249"},
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

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.8469/4.22489999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            TestChallenge("Journey", "EAS with FirstLastMile walk, crow inbetween, Sint-Niklaas (OSM) -> BxlN (OSM)",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.17236/4.14395999999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860439/4.35865000000001",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );
            TestChallenge("Journey", "PCS with FirstLastMile walk, crow inbetween, Sint-Niklaas (OSM) -> BxlN (OSM)",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.17236/4.14395999999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860439/4.35865000000001",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                },
                maxTimeAllowed: 4000
            );


            TestChallenge("Journey", "EAS with FirstLastMile ebike, Sint-Niklaas (OSM) -> BxlN OSM",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.17236/4.14395999999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860439/4.35865000000001",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "EAS with FirstLastMile ebike, Sint-Niklaas (OSM) -> BxlN OSM",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.17236/4.14395999999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.860439/4.35865000000001",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );


            TestChallenge("Journey", "PCS with FirstLastMile ebike, BxlN (OSM) -> Aalst (OSM)",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/50.86051/4.35839899999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.942586/4.038028",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "EAS with FirstLastMile ebike, BxlN (OSM) -> Aalst (OSM)",
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
                        AssertEqual("https://www.openstreetmap.org/#map=19/50.86051/4.35839899999999",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/50.942586/4.038028",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            );

            TestChallenge("Journey", "PCS with FirstLastMile ebike, WTC3 BxlN (OSM) -> Gent (OSM)",
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
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/50.86094/4.35405",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("https://www.openstreetmap.org/#map=19/51.03465/3.70831999999999",
                            j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                },
                maxTimeAllowed: 3000
            );

            TestChallenge(
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
            TestChallenge(
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


            TestChallenge("journey", "EAS (cycle, crow, walk), Beveren (OSM) -> St Niklaas (OSM)",
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

            TestChallenge("journey", "PCS (cycle, crow, walk), Beveren (OSM) -> St Niklaas (OSM)",
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


            TestChallenge("Journey", "EAS (cycle, crow, walk), Antw Berchem (OSM) -> Brussel Warandepark (OSM)",
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
            TestChallenge("Journey", "PCS (cycle, crow, walk), Antw Berchem (OSM) -> Brussel Warandepark (OSM)",
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

            TestChallenge("journey",
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
        }
    }
}