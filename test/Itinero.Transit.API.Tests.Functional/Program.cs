using Itinero.Transit.Api;

namespace Itinero.Transit.API.Tests.Functional
{
    static class Program
    {
        private static string _host = "http://localhost:5000";

        static void Main(string[] args)
        {
            Startup.ConfigureLogging();


            new PerfTest().Run(PerfTest.Sources);
        }
    }
}