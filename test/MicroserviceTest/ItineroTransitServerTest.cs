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
            {"prod", "https://routing.anyways.eu/transitapi"},
            {"production", "https://routing.anyways.eu/transitapi"}
        };


        public ItineroTransitServerTest() : base(
            "Itinero-Transit Tester",
            "production",
            knownUrls,
            defaultTimeout: 1000)
        {
        }

        protected override void RunTests()
        {
            // Is the server online?
            Challenge("status", "IsOnline",
                property: jobj =>
                {
                    AssertTrue(jobj["online"].Value<bool>(), "Not online");
                    var loadedWindows = jobj["loadedTimeWindows"].Count();
                    AssertTrue(loadedWindows > 0, "Not all operators are loaded");
                }
            );

            // Do we find stops?
            Challenge("LocationsByName", "Search Brugge",
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

            // Do we find stops with spelling errors?
            Challenge("LocationsByName", "Search Brugg",
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


            // Do we find locations around a stop?
            Challenge("LocationsAround", "Search Locations around",
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


            // Do we find information on a location
            Challenge("Location", "Get info about",
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


            Challenge("Location/Connections", "Test connections of station (regr)",
                new Dictionary<string, string>
                {
                    {"id", "http://irail.be/stations/NMBS/008891009"}
                },
                jobj =>
                {
                    AssertEqual("http://irail.be/stations/NMBS/008891009", jobj["location"]["id"]);
                    AssertTrue(jobj["segments"].Any());
                }
            );


            // Can we find journeys? Brugge => Gent-St-Pieters
            Challenge("Journey", "NMBS -> NMBS",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", DateTime.Now.ToString("s")}
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

            Challenge("Journey", "PARETO Brugge NMBS -> NMBS (Friendly)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", DateTime.Now.ToString("s")},
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

            // Can we find journeys from one OSM location to a stop with crows flight?
            // Close to Brugge station => Gent-Sint-Pieters
            Challenge("Journey", "OSM -> NMBS (friendly)",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", DateTime.Now.ToString("s")}
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

            // And swap them around...
            Challenge("Journey", "NMBS -> OSM (friendly)",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008892007"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"departure", DateTime.Now.ToString("s")}
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

            // And between two floating points
            Challenge("Journey", "OSM -> OSM",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges station
                    {"departure", DateTime.Now.ToString("s")}
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

            Challenge("Journey", "Pareto OSM -> OSM",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges station
                    {"departure", DateTime.Now.ToString("s")},
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


            // Check a journey that doesn't need PT
            Challenge("Journey", "Walk only",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges
                    {"to", "http://irail.be/stations/NMBS/008891009"}, // station of bruges
                    {"departure", DateTime.Now.ToString("s")}
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

            Challenge("Journey", "FirstLastMile, crow inbetween (rijselsestraat -> GentSP NMBS)",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"
                    }, // Behind station of bruges, Rijselsestraat
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", $"{DateTime.Now:s}Z"},
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


            Challenge("Journey", "FirstLastMile, crow inbetween (rijselsestraat -> De Sterre)",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"
                    }, // Behind station of bruges, Rijselsestraat
                    {"to", "https://www.openstreetmap.org/#map=14/51.0250/3.7129"}, // To Ghent: De Sterre
                    {"departure", $"{DateTime.Now:s}Z"},
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

            Challenge("Journey", "FirstLastMile, crow inbetween (rijselsestraat -> Close to GhentSP)",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"
                    }, // Behind station of bruges, Rijselsestraat
                    {"to", "https://www.openstreetmap.org/#map=16/51.0374/3.7151"},
                    {"departure", $"{DateTime.Now:s}Z"},
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

            Challenge("Journey", "Adinkerke -> Gouvy",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=15/51.0858/2.6017"
                    }, // Adinkerke/De Panne
                    {"to", "https://www.openstreetmap.org/#map=14/50.1886/5.9543"}, // Gouvy

                    {"departure", $"{DateTime.Now:s}Z"},
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


            Challenge("Journey", "PARETO - Poperinge -> Brussels",
                new Dictionary<string, string>
                {
                    {
                        "from", "http://irail.be/stations/NMBS/008896735"
                    }, //"https://www.openstreetmap.org/#map=14/50.8535/2.7345"},
                    {"to", "https://www.openstreetmap.org/#map=11/50.8469/4.2249"},

                    {"departure", $"{DateTime.Now:s}Z"},
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
                },
                maxTimeAllowed: 2000
            );
            Challenge("Journey", "Sint-Niklaas OSM -> BxlN OSM (Regr test)",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=19/51.17236/4.14396"
                    },
                    {"to", "https://www.openstreetmap.org/#map=19/50.86044/4.35865"},

                    {"departure", $"{DateTime.Now:s}Z"},
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


            Challenge("Journey", "Sint-Niklaas OSM -> BxlN OSM (Regr test with ebike)",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=19/51.17236/4.14396"
                    },
                    {"to", "https://www.openstreetmap.org/#map=19/50.86044/4.35865"},

                    {"departure", $"{DateTime.Now:s}Z"},
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


            Challenge("Journey", "Regr test 0",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/50.86051035579558/4.358399302117419"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.942586962931955/4.038028645195254"},

                    {"departure", $"{DateTime.Now:s}Z"},
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

            Challenge("Journey", "Regr test 1",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=17/50.86094/4.35405"
                    }, //Not working: "https://www.openstreetmap.org/#map=19/50.8600527/4.3556522"}, 
                    {
                        "to", "https://www.openstreetmap.org/#map=19/51.03465/3.70832"
                    }, //"http://irail.be/stations/NMBS/008892007"}, //

                    {"departure", $"{DateTime.Now:s}Z"},

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
                }
            );


            Challenge(
                "journey",
                "Regr test with real URL 1",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.1951297895458/3.214899786053735"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.84388967236137/4.354740038815095"},
                    {"departure", "2019-07-17T08:35:00.000Z"},
                    {"inBetweenOsmProfile", "pedestrian"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "speedPedelec"},
                    {"firstMileSearchDistance", "50000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"}
                },
                jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); }, 4000
            );


            Challenge("journey", "Regr test with real URL 2",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.21523909670509/4.268520576417103"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.16509172615645/4.135866853010015"},
                    {"departure", "2019-07-22T15:23:00.000Z"},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "500"},
                    {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "10000"},
                    {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"},
                    {"multipleOptions", "true"}
                }, jobj => { AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null"); }, maxTimeAllowed: 4000);


            Challenge("Journey", "Regr test with real URL 3",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.199993/4.431101"},
                    {"to", "https://www.openstreetmap.org/#map=19/50.843183/4.371755"},
                    {"departure", "2019-07-24T10:05:00.000Z"},
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

            Challenge("journey", "Regr 4 with direct cycling path",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.270256567260844/3.0617134555123755"}, // Nieuwmunster
                    {"to", "https://www.openstreetmap.org/#map=16/51.2646/3.1546"}, // Zuienkerke
                    {"departure", "2019-07-24T10:50:00.000Z"},
                    {"inBetweenOsmProfile", "crowsflight"},
                    {"inBetweenSearchDistance", "0"}, {"firstMileOsmProfile", "bicycle"},
                    {"firstMileSearchDistance", "20000"}, {"lastMileOsmProfile", "pedestrian"},
                    {"lastMileSearchDistance", "10000"}
                },
                jobj =>
                {
                    AssertNotNullOrEmpty(jobj["journeys"], "Journeys are null");
                },
                maxTimeAllowed: 4000);
        }
    }
}