using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.API.Tests.Functional
{
    static class Program
    {
        private static string _host = "http://localhost:5000";

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                _host = args[0];
            }

            Console.WriteLine("Host is " + _host);
            if (!_host.EndsWith('/'))
            {
                _host += '/';
            }

            Console.WriteLine("Running integration tests");
            Challenge("status",
                jobj =>
                {
                    AssertTrue(jobj["online"].Value<bool>(), "Not online");
                    var loadedWindows = jobj["loadedTimeWindows"].Count();
                    AssertTrue(loadedWindows == 3 || loadedWindows == 8, "Not all operators are loaded");
                });

            Challenge("LocationsByName?name=Brugge",
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

            Challenge("LocationsByName?name=Brugg",
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


            Challenge("LocationsAround?lat=51.1978&lon=3.2184&distance=500",
                jobj =>
                {
                    var ids = jobj.Select(location => location["id"]);
                    AssertTrue(
                        ids.Contains("https://www.openstreetmap.org/node/6348496391"), "CentrumShuttle stop not found");
                    AssertTrue(
                        ids.Contains("http://irail.be/stations/NMBS/008891009"), "Station Brugge not found");
                }
            );


            Challenge("Location?id=http%3A%2F%2Firail.be%2Fstations%2FNMBS%2F008891009",
                jobj =>
                {
                    AssertEqual("51.1972295551607", jobj["lat"].Value<string>());
                    AssertEqual(3.216724991798401, jobj["lon"].Value<double>());
                    AssertEqual("http://irail.be/stations/NMBS/008891009", jobj["id"].Value<string>());
                    AssertEqual("Brugge", jobj["name"].Value<string>());
                    AssertEqual("Bruges", jobj["translatedNames"]["fr"].Value<string>());
                }
            );

            Challenge("Location?id=http%3A%2F%2Firail.be%2Fstations%2FNMBS%2F008891009",
                jobj =>
                {
                    AssertEqual("51.1972295551607", jobj["lat"].Value<string>());
                    AssertEqual(3.216724991798401, jobj["lon"].Value<double>());
                    AssertEqual("http://irail.be/stations/NMBS/008891009", jobj["id"].Value<string>());
                    AssertEqual("Brugge", jobj["name"].Value<string>());
                    AssertEqual("Bruges", jobj["translatedNames"]["fr"].Value<string>());
                }
            );


            Challenge("Journey?from=http://irail.be/stations/NMBS/008891009" +
                      "&to=http://irail.be/stations/NMBS/008892007" +
                      $"&departure={DateTime.Now:s}Z" +
                      "&internalTransferTime=180&transferPenalty=300&prune=true",
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

        private static void Challenge(string urlParams,
            Action<JToken> property)
        {
            ChallengeAsync(urlParams, property).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static bool _failed;

        private static async Task ChallengeAsync(string urlParams,
            Action<JToken> property)
        {
            Console.Write(" ...          Running test with URL " +
                          urlParams.Substring(0, Math.Min(80, urlParams.Length)));
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
                Console.Write(" " + e.Message);
                Console.WriteLine($"\rFAIL {timing}");
                _failed = true;
            }
        }
    }
}