using System;
using Itinero.Transit.Api;

namespace Itinero.Transit.API.Tests.Functional
{
    static class Program
    {
        private static string _host = "http://localhost:5000";

        static void Main(string[] args)
        {
            Startup.ConfigureLogging();

            if (args.Length <= 0)
            {
                Console.WriteLine("Either use --perf or specify a hostname");
            }

            ;
            if (args[0].Equals("--perf"))
            {
                new PerfTest().Run(PerfTest.Sources);
            }
            else
            {
                _host = args[0];
                new ServerTest(_host).RunTests();
            }
        }
    }
}