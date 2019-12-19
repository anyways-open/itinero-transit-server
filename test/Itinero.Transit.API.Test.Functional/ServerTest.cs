using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Itinero.Transit.API.Tests.Functional
{
    public struct Challenge
    {
        public uint MaxTimeAllowed { get; }
        public string Name { get; }
        public string Url { get; }
        public Action<JToken> Property { get; }
        
        public Action<string> HandleErrorMessage { get;  }

        public Challenge(string name, string url, Action<JToken> property, uint maxTimeAllowed, Action<string> handleErrorMessage = null)
        {
            MaxTimeAllowed = maxTimeAllowed;
            HandleErrorMessage = handleErrorMessage ?? (msg => throw new Exception($"Got error message: {msg}"));
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
            var timeOutCount = 0;
            var count = 0;
            foreach (var challenge in challenges)
            {
                _errorMessages = new List<string>();
                Console.Write($"{(challenges.Count - count):D4} Running {challenge.Name}");
                count++;

                var startCh = DateTime.Now;
                try
                {
                    ChallengeAsync(host, challenge, ignoreTimouts ? 120000 : challenge.MaxTimeAllowed).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    _errorMessages.Add(e.Message);
                }

                var endCh = DateTime.Now;
                var timeNeeded = (int) (endCh - startCh).TotalMilliseconds;


                var didTimeout = PrintOutput(host, ignoreTimouts, timeNeeded, challenge, count,
                    ref failCount);
                if (didTimeout)
                {
                    timeOutCount++;
                }
            }

            var end = DateTime.Now;
            Console.WriteLine($"Testing took {(end - start).Seconds} seconds");

            
            if (failCount > 0)
            {
                var msg = $"{failCount}/{challenges.Count} tests failed - {timeOutCount} did time out.";
                WriteErrHard(msg);
                Console.WriteLine();
                throw new Exception(msg);
            }

            if (timeOutCount > 0)
            {
                var msg = $"{timeOutCount}/{challenges.Count} tests did timeout.";

                WriteWarnHard(msg);
                Console.WriteLine();
                if (!ignoreTimouts)
                {
                    throw new Exception(msg);
                }
            }
            else
            {
                WriteGoodHard($"All {challenges.Count} tests successful.");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Print output, returns true if it did timeout
        /// </summary>
        private bool PrintOutput(string host, bool ignoreTimouts, int timeNeeded, Challenge challenge,
            int count, ref int failCount)
        {
            var secs = timeNeeded / 1000;
            var ms = timeNeeded % 1000;


            void WriteSeconds()
            {
                var timing = $"{secs}.{ms:000}s";

                var err = challenge.MaxTimeAllowed;
                if (err == 0)
                {
                    err = 5000;
                }

                var warnHard = err * 3 / 4;
                var warn = err / 2;
                var veryGood = err / 4;

                if (timeNeeded > err)
                {
                    WriteErrHard(timing);
                }
                else if (timeNeeded > warnHard)
                {
                    WriteWarnHard(timing);
                }
                else if (timeNeeded > warn)
                {
                    WriteWarn(timing);
                }
                else if (timeNeeded > veryGood)
                {
                    WriteGood(timing);
                }
                else
                {
                    WriteGoodHard(timing);
                }

                Console.Write("  ");
            }

            var timedOut = challenge.MaxTimeAllowed != 0 &&
                           timeNeeded > challenge.MaxTimeAllowed;


            Console.Write("\r");
            if (_errorMessages.Any())
            {
                failCount++;
                WriteErrHard("FAIL ");
            }
            else if (timedOut)
            {
                if (!ignoreTimouts)
                {
                    failCount++;
                }

                WriteWarnHard("TIME ");
            }
            else
            {
                WriteGood(" OK  ");
            }

            WriteSeconds();

            if (!_errorMessages.Any())
            {
                Console.WriteLine($"{challenge.Name}");
            }
            else
            {
                Console.WriteLine(
                    $"{challenge.Name}\n     An error occured while running test {count} against URL\n    {host}/{challenge.Url}");

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

            return timedOut;
        }

        /// <summary>
        /// Executes the challenge against the given host.
        /// Returns the needed milliseconds
        /// </summary>
        public async Task ChallengeAsync(
            string host,
            Challenge challenge,
            uint timoutInMillis)
        {
            if (!host.EndsWith('/'))
            {
                host += "/";
            }

            var urlParams = challenge.Url;
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(timoutInMillis * 2)
            };
            var uri = host + urlParams;
            var response = await client.GetAsync(uri).ConfigureAwait(false);

            if (response == null)
            {
                throw new HttpRequestException("No response on " + uri);
            }

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                challenge.HandleErrorMessage("Upstream server error: "+content);
                return ;
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

        protected Challenge Challenge(string endpoint,
            string name,
            Dictionary<string, string> keyValues = null,
            Action<JToken> property = null,
            uint maxTimeAllowed = 0,
            Action<string> handleError = null)
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

            var challenge = new Challenge(name, url, property, maxTimeAllowed, handleError);
            _challenges?.Add(challenge);
            return challenge;
        }

        // ------------------ Boring 'Asserts' down here --------------- //
        /*
         * For the asserts, we use another 'inversion of control'
         * A challenge might check multiple properties, but even if one fails, no exception is thrown
         * Exceptions are only thrown at the end of the challenge, to aggregate multiple failures
         *
         * The '_errorMessages'-list is where all errors are gathered
         */

        public void AssertEqual<T>(T expected, T val, string errMessage = "")
        {
            if (!expected.Equals(val))
            {
                _errorMessages.Add($"Values are not the same. Expected {expected} but got {val}. {errMessage}");
            }
        }


        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        public void AssertTrue(bool val, string errMessage = "Expected True")
        {
            if (!val)
            {
                _errorMessages.Add(errMessage);
            }
        }

        public void AssertNotNull(object val, string errMessage = "Value is null")
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
            else if (!val.HasValues)
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