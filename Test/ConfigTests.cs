using Itinero.Transit.Api.Logic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Test
{
    public class ConfigTests
    {
        [Fact]
        public void TestConfig()
        {
            var dirsToTest = new[]
            {
                // Working dir: itinero-transit-server/Test/bin/Debug/netcoreapp2.2
                "src/Itinero.Transit.Api/appsettings.json",
                "src/Itinero.Transit.Api/appsettings.documentation.json",
                "src/Itinero.Transit.Api/appsettings.Docker.json"
            };

            foreach (var s in dirsToTest)
            {
                var path = "/home/pietervdvn/git/itinero-transit-server/" + s;
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile(path);
                configuration.Build();

                TransitDbFactory.CreateTransitDbs(configuration.Build().GetSection("TransitDb"), true);
            }
        }
    }
}