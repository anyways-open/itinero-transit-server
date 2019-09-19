using System;
using System.Collections.Generic;
using System.Linq;

namespace Itinero.Transit.API.Tests.Functional
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerTest serverTest = new ItineroTransitServerTest();

            if (args.Length == 0)
            {
                serverTest.RunTestsAgainst(serverTest.DefaultHost);
                return;
            }

            if (new[] {"help", "--help", "-?", "--?", "-h"}.Contains(args[0]))
            {
                var knownUrls = "";
                foreach (var pair in serverTest.KnownUrls)
                {
                    if (pair.Key.Equals(serverTest.DefaultHost))
                    {
                        knownUrls += "* ";
                    }
                    else
                    {
                        knownUrls += "  ";
                    }

                    knownUrls += $"{pair.Key}";
                    if (pair.Key.Length < 8)
                    {
                        knownUrls += "\t";
                    }

                    knownUrls += $"\t{pair.Value}";


                    knownUrls += "\n";
                }

                Console.WriteLine("Microservice tester 0.1\n" +
                                  "Run without argument to apply the default test\n" +
                                  "Or use an URL to run the test suite against the specified host\n" +
                                  "Some servertests have shorthands defined, which can be used instead of the URL\n" +
                                  "\n" +
                                  "\n Use '--help' to get this help text" +
                                  "\n Use '--fail' to mimic failing behaviour. Useful to test if error messages come through etc" +
                                  "\n" +
                                  $"This microservice tester is loaded with {serverTest.Name}\n" +
                                  $"Known urls are:\n" +
                                  $"{knownUrls}");
                return;
            }


            if (args[0].Equals("--fail"))
            {
                serverTest = new Failer();
            }


            if (args.Length == 3 && args[1].Equals("--only-run"))
            {
                var focus = int.Parse(args[2]) - 1;
                serverTest.RunTestsAgainst(args[0], focus);
                return;
            }

            var ignoreTimout = args.Length >= 2 && args[1].Equals("--ignore-timeout");
            if (ignoreTimout)
            {
                ServerTest.WriteWarn("Running with elevated timeout of 2 minutes");
                Console.WriteLine();
            }
            serverTest.RunTestsAgainst(args[0], null, ignoreTimout);
        }
    }

    class Failer : ServerTest
    {
        public Failer() :
            base("Fails", "--fail", new Dictionary<string, string>{{"--fail","https://www.anyways.eu"}})
        {
        }

        protected override void RunTests()
        {
            Challenge("", "This test always fails", new Dictionary<string, string>(),
                jobj => { AssertTrue(false, errMessage: "This one always fails"); });
        }
    }
}