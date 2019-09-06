using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MicroserviceTest
{
    public struct Challenge
    {
        public uint MaxTimeAllowed { get; }
        public string Name { get; }
        public string Url { get; }
        public Action<JToken> Property { get; }

        public Challenge(string name, string url, Action<JToken> property, uint maxTimeAllowed)
        {
            MaxTimeAllowed = maxTimeAllowed;
            Name = name;
            Url = url;
            Property = property;
        }
    }

    public abstract class ServerTest
    {
        public readonly string Name;
        public readonly string DefaultHost;
        public readonly Dictionary<string, string> KnownUrls;
        private readonly uint _defaultTimeout;
        public bool FailFast;

        public ServerTest(
            string name,
            string defaultHost,
            Dictionary<string, string> knownUrls,
            uint defaultTimeout = 0,
            bool failFast = false)
        {
            Name = name;
            DefaultHost = defaultHost;
            KnownUrls = knownUrls;
            _defaultTimeout = defaultTimeout;
            FailFast = failFast;
        }

        private List<string> _errorMessages;

        public void RunTestsAgainst(string host = null, int? onlyRunThisTest = null, bool ignoreTimouts = false)
        {
            host = host ?? DefaultHost;
            if (KnownUrls.ContainsKey(host))
            {
                host = KnownUrls[host];
            }

            if (!host.EndsWith('/'))
            {
                host += "/";
            }


            var start = DateTime.Now;

            var challenges = CreateChallenges();

            if (onlyRunThisTest != null)
            {
                var focusedTest = challenges[onlyRunThisTest.Value];
                challenges = new List<Challenge> {focusedTest};
            }

            var sumValid = true;
            uint sum = 0;
            foreach (var challenge in challenges)
            {
                if (challenge.MaxTimeAllowed == 0 || challenge.MaxTimeAllowed == uint.MaxValue)
                {
                    sumValid = false;
                    break;
                }

                sum += challenge.MaxTimeAllowed;
            }

            var tm = "";

            if (sumValid)
            {
                tm = $"taking at most {sum / 1000}s";
            }

            Console.WriteLine(
                $"Running {Name} with {challenges.Count} tests {tm} against default host '{host}'");
            var failCount = 0;
            var count = 0;
            foreach (var challenge in challenges)
            {
                _errorMessages = new List<string>();
                Console.Write($"{(challenges.Count - count):D4} Running {challenge.Name}");
                count++;

                var startCH = DateTime.Now;
                try
                {
                    var x = ChallengeAsync(host, challenge, ignoreTimouts ? 120000 : challenge.MaxTimeAllowed).Result;
                }
                catch (Exception e)
                {
                    _errorMessages.Add(e.Message);
                }

                var endCh = DateTime.Now;
                var timeNeeded = (endCh - startCH).Milliseconds;


                if (challenge.MaxTimeAllowed != 0 &&
                    timeNeeded > challenge.MaxTimeAllowed)
                {
                    var msg = $"Timeout: this test is only allowed to run for {challenge.MaxTimeAllowed}ms";
                    if (ignoreTimouts)
                    {
                        WriteWarnHard(msg);
                    }
                    else
                    {
                        _errorMessages.Add(msg);
                    }
                }

                var secs = timeNeeded / 1000;
                var ms = timeNeeded % 1000;


                void WriteSeconds()
                {
                    var timing = $" {secs}.{ms:000}s: ";

                    var err = challenge.MaxTimeAllowed;
                    if (err == 0)
                    {
                        err = 5000;
                    }

                    var warn = err * 3 / 4;

                    if (timeNeeded > err)
                    {
                        WriteErr(timing);
                    }
                    else if (timeNeeded > warn)
                    {
                        WriteWarn(timing);
                    }
                    else if (challenge.MaxTimeAllowed == 0)
                    {
                        Console.Write(timing);
                    }
                    else
                    {
                        WriteGood(timing);
                    }
                }


                Console.Write("\r");
                if (!_errorMessages.Any())
                {
                    WriteGood(" OK ");
                    WriteSeconds();
                    Console.WriteLine($"{challenge.Name}");
                }
                else
                {
                    failCount++;
                    WriteErrHard("FAIL");
                    WriteSeconds();

                    Console.WriteLine(
                        $"{challenge.Name}\n     An error occured while running test {count} against URL\n    {host}{challenge.Url}");

                    foreach (var err in _errorMessages)
                    {
                        Console.WriteLine("     " + err);
                    }

                    Console.WriteLine();

                    if (FailFast)
                    {
                        var errMsg = string.Join(", ", _errorMessages);
                        throw new Exception(errMsg);
                    }
                }
            }

            var end = DateTime.Now;
            Console.WriteLine($"Testing took {(end - start).Seconds} seconds");

            if (failCount > 0)
            {
                var msg = $"{failCount}/{challenges.Count} tests failed.";
                WriteErrHard(msg);
                Console.WriteLine();
                throw new Exception(msg);
            }
            else
            {
                WriteGoodHard($"All {challenges.Count} tests successful.");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Executes the challenge against the given host.
        /// Returns the needed milliseconds
        /// </summary>
        private async Task<int> ChallengeAsync(
            string host,
            Challenge challenge,
            uint timoutInMillis)
        {
            var urlParams = challenge.Url;
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(timoutInMillis*2)
            };
            var uri = host + urlParams;
            var response = await client.GetAsync(uri).ConfigureAwait(false);
            if (response == null || !response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("Could not open " + uri);
            }

            var data = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            JToken json;
            try
            {
                json = JToken.Parse(data);
            }
            catch
            {
                throw new HttpRequestException("Result is not a valid JSON-file");
            }


            try
            {
                challenge.Property(json);
            }
            catch (Exception e)
            {
                _errorMessages.Add(e.Message);
            }

            return 0;
        }

        protected abstract void RunTests();


        private List<Challenge> _challenges;

        private List<Challenge> CreateChallenges()
        {
            _challenges = new List<Challenge>();
            // Gathers all challenges in the '_challenges'-list
            // This is a bit an inversion of control and abuse of style, but at least we get a list of challenges afterwards, so we can program functionally for the rest of the code 
            RunTests();
            return _challenges;
        }


        /// <summary>
        /// Execute or create a challenge
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="name"></param>
        /// <param name="keyValues"></param>
        /// <param name="property"></param>
        /// <param name="maxTimeAllowed">In seconds</param>
        internal void Challenge(string endpoint,
            string name,
            Dictionary<string, string> keyValues = null,
            Action<JToken> property = null,
            uint maxTimeAllowed = 0)
        {
            keyValues = keyValues ?? new Dictionary<string, string>();
            property = property ?? (jobj => { });
            maxTimeAllowed = maxTimeAllowed == 0 ? _defaultTimeout : maxTimeAllowed;

            var parameters = string.Join("&", keyValues.Select(kv => kv.Key + "=" + Uri.EscapeDataString(kv.Value)));
            var url = endpoint;
            if (parameters.Any())
            {
                url = endpoint + "?" + parameters;
            }

            var challenge = new Challenge(name, url, property, maxTimeAllowed);
            _challenges.Add(challenge);
        }

        // ------------------ Boring 'Asserts' down here --------------- //
        /*
         * For the asserts, we use another 'inversion of control'
         * A challenge might check multiple properties, but even if one fails, no exception is thrown
         * Exceptions are only thrown at the end of the challenge, to aggregate multiple failures
         *
         * The '_errorMessages'-list is where all errors are gathered
         */

        internal void AssertEqual<T>(T expected, T val, string errMessage = "")
        {
            if (!expected.Equals(val))
            {
                _errorMessages.Add($"Values are not the same. Expected {expected} but got {val}. {errMessage}");
            }
        }


        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        internal void AssertTrue(bool val, string errMessage = "Expected True")
        {
            if (!val)
            {
                _errorMessages.Add(errMessage);
            }
        }

        internal void AssertNotNull(object val, string errMessage = "Value is null")
        {
            if (val == null)
            {
                _errorMessages.Add(errMessage);
            }
        }

        internal void AssertNotNullOrEmpty(JToken val, string errMessage = "Value is null or empty")
        {
            if (val == null)
            {
                _errorMessages.Add(errMessage);
            }

            if (!val.HasValues)
            {
                _errorMessages.Add(errMessage);
            }
        }

        internal static void WriteErrHard(string msg)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write(msg);
            Console.ResetColor();
        }

        internal static void WriteErr(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(msg);
            Console.ResetColor();
        }

        internal static void WriteWarn(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(msg);
            Console.ResetColor();
        }

        internal static void WriteWarnHard(string msg)
        {
            Console.BackgroundColor
                = ConsoleColor.Yellow;
            Console.ForegroundColor
                = ConsoleColor.Black;
            Console.Write(msg);
            Console.ResetColor();
        }

        internal static void WriteGood(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(msg);
            Console.ResetColor();
        }

        internal static void WriteGoodHard(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.Write(msg);
            Console.ResetColor();
        }
    }
}