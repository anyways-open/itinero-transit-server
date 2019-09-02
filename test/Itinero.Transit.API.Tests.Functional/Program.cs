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
                new ServerTest("http://localhost:5000").RunTests();
                return;
            }

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