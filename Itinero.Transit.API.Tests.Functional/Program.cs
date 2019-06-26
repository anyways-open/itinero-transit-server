using Itinero.Transit.Api;

namespace Itinero.Transit.API.Tests.Functional
{
    static class Program
    {
        private static string _host = "http://localhost:5000";

        static void Main(string[] args)
        {
            Startup.ConfigureLogging();
            
            if (args.Length > 0)
            {
                _host = args[0];
            }
            
            new PerfTest().Run(PerfTest.Sources);

        }
    }
}