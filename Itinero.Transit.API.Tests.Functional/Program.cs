using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.API.Tests.Functional
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running integration tests");
            Challenge("status",
                jobj =>
                {
                    AssertTrue(jobj["online"].Value<bool>(), "Not online");
                    AssertEqual(8, jobj["loadedTimeWindows"].Count(), "Not all operators are loaded");
                });

            Challenge("LocationsByName?name=Brugge",
                jobj =>
                {
                    // The first entry should respond with NMBS-station 'Brugge'
                    var stationBrugge = jobj[0];
                    var result = true;
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
                    // The first entry should respond with K&R CentrumShuttle'
                    var kr = jobj[0];
                    AssertEqual("https://www.openstreetmap.org/node/6348496391",
                        kr["id"].Value<string>(),
                        "Search returned something else then the main K&R of the CentrumShuttle");


                    // The second entry should respond with NMBS-station 'Brugge'
                    var stationBrugge = jobj[1];
                    AssertEqual("http://irail.be/stations/NMBS/008891009",
                        stationBrugge["id"].Value<string>(),
                        "Search returned something else then the main station of Bruges");
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
            
            
            
            if(failed){throw  new Exception("Some tests failed");}
        }

        private static void AssertEqual<T>(T expected, T val, string errMessage = "")
        {
            if (!expected.Equals(val))
            {
                throw new Exception($"Values are not the same. Expected {expected} but got {val}. {errMessage}");
            }
        }

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

        private static bool failed = false;
        private static async Task ChallengeAsync(string urlParams,
            Action<JToken> property)
        {
            Console.Write(" ... Running test with URL " + urlParams);
            var _client = new HttpClient();
            string uri = "http://localhost:5000/" + urlParams;
            var response = await _client.GetAsync(uri).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Could not open " + uri);
            }

            var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var json = JToken.Parse(data);

            try
            {

                property(json);
                Console.WriteLine("\r[OK] ");
            }
            catch(Exception e)
            {
                Console.Write(" "+e.Message);
                Console.WriteLine("\rFAIL ");
                failed = true;
            }
        }
    }
}