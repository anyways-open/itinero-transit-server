using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.API.Tests.Functional
{
    public class ServerTest
    {
        private readonly string _host;

        public ServerTest(string host)
        {
            _host = host;
            if (!_host.EndsWith('/'))
            {
                _host += '/';
            }

            Console.WriteLine("Host is " + _host);
        }

        public void RunTests()
        {
            Console.WriteLine("Running integration tests");
            File.WriteAllText("testUrls", "Testing urls are: \n");
            // Is the server online?
            Challenge("status",
                new Dictionary<string, string>(),
                jobj =>
                {
                    AssertTrue(jobj["online"].Value<bool>(), "Not online");
                    var loadedWindows = jobj["loadedTimeWindows"].Count();
                    AssertTrue(loadedWindows == 2 || loadedWindows == 8, "Not all operators are loaded");
                });

            // Do we find stops?
            Challenge("LocationsByName",
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
            Challenge("LocationsByName",
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
            Challenge("LocationsAround",
                new Dictionary<string, string>()
                {
                    {"lat", "51.1978"},
                    {"lon", "3.2184"},
                    {"distance", "500"}
                },
                jobj =>
                {
                    var ids = jobj.Select(location => location["id"]).ToList();
                    AssertTrue(
                        ids.Contains("https://www.openstreetmap.org/node/6348496391"), "CentrumShuttle stop not found");
                    AssertTrue(
                        ids.Contains("http://irail.be/stations/NMBS/008891009"), "Station Brugge not found");
                }
            );


            // Do we find information on a location
            Challenge("Location",
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


            // Can we find journeys? Brugge => Gent-St-Pieters
            Challenge("Journey",
                new Dictionary<string, string>
                {
                    {"from", "http://irail.be/stations/NMBS/008891009"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", DateTime.Now.ToString("s")}
                },
                jobj =>
                {
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
            Challenge("Journey",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"},
                    {"to", "http://irail.be/stations/NMBS/008892007"},
                    {"departure", DateTime.Now.ToString("s")}
                },
                jobj =>
                {
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
            Challenge("Journey",
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
            Challenge("Journey",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=15/51.0359/3.7108"},
                    {"to", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges station
                    {"departure", DateTime.Now.ToString("s")}
                },
                jobj =>
                {
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
            Challenge("Journey",
                new Dictionary<string, string>
                {
                    {"from", "https://www.openstreetmap.org/#map=19/51.19764/3.21847"}, // Close to bruges
                    {"to", "http://irail.be/stations/NMBS/008891009"}, // station of bruges
                    {"departure", DateTime.Now.ToString("s")}
                },
                jobj =>
                {
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
            //*/

            // Can we find journeys from one OSM location to a stop with ?
            // With the osm-pedestrian profile
            // Close to Brugge station => Gent-Sint-Pieters
            Challenge("Journey",
                new Dictionary<string, string>
                {
                    {
                        "from", "https://www.openstreetmap.org/#map=17/51.21577/3.21823"
                    }, // Behind station of bruges, Rijselsestraat
                    {"to", "https://www.openstreetmap.org/#map=14/51.0250/3.7129"}, // To Ghent
                    {"departure", $"{DateTime.Now:s}Z"},
                    {
                        "walksGeneratorDescription",
                        "https://openplanner.team/itinero-transit/walks/firstLastMile&" +
                        "default=" +
                        Uri.EscapeDataString(
                            "https://openplanner.team/itinero-transit/walks/crowsflight&maxDistance=500&speed=1.4") +
                        "&firstMile=" +
                        Uri.EscapeDataString(
                            "https://openplanner.team/itinero-transit/walks/osm&maxDistance=5000&profile=pedestrian") +
                        "&lastMile=" +
                        Uri.EscapeDataString(
                            "https://openplanner.team/itinero-transit/walks/osm&maxDistance=5000&profile=pedestrian")
                    }
                },    
                jobj =>
                {
                    AssertTrue(jobj["journeys"].Any(), "No journeys found");
                    foreach (var j in jobj["journeys"])
                    {
                        AssertEqual("https://www.openstreetmap.org/#map=19/51.21577/3.21823000000001",
                            j["departure"]["location"]["id"],
                            "Wrong departure stations");

                        AssertEqual("http://irail.be/stations/NMBS/008892007", j["arrival"]["location"]["id"],
                            "Wrong arrival stations");
                    }
                }
            ); //*/


            if (_failed)
            {
                throw new Exception("Some tests failed");
            }
        }

        private static void AssertEqual<T>(T expected, T val, string errMessage = "")
        {
            if (!expected.Equals(val))
            {
                throw new Exception($"Values are not the same. Expected {expected} but got {val}. {errMessage}");
            }
        }


        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void AssertTrue(bool val, string errMessage = "Expected True")
        {
            if (!val)
            {
                throw new Exception(errMessage);
            }
        }

        private void Challenge(string endpoint,
            Dictionary<string, string> keyValues,
            Action<JToken> property)
        {
            var parameters = string.Join("&", keyValues.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value)));
            var url = endpoint + "?" + parameters;

            ChallengeAsync(url, property).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static bool _failed;

        private async Task ChallengeAsync(string urlParams,
            Action<JToken> property)
        {
            Console.Write(" ...          Running test with URL " +
                          urlParams.Substring(0, Math.Min(80, urlParams.Length)));
            File.AppendAllText("testUrls", urlParams+"\n");
            var start = DateTime.Now;
            var client = new HttpClient();
            var uri = _host + urlParams;
            var response = await client.GetAsync(uri).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Could not open " + uri);
            }

            var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var json = JToken.Parse(data);

            var end = DateTime.Now;
            var time = (int) (end - start).TotalMilliseconds;
            var timing = $"{time}ms";
            if (time > 1000)
            {
                time /= 1000;
                timing = $"{time}s";
            }


            try
            {
                property(json);
                Console.WriteLine($"\r[OK] {timing}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"\rFAIL {timing}");
                Console.WriteLine(" " + e.Message);
                _failed = true;
            }
        }
    }
}