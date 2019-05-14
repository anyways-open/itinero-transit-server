using System;
using Itinero.Transit.Api.Logic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Test
{
    public class UnitTest1
    {
        [Fact]
        public void TestConfig()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("/home/pietervdvn/git/itinero-transit-server/src/Itinero.Transit.Api/appsettings.json");
            configuration.Build();

            TransitDbFactory.CreateTransitDbs(configuration.Build().GetSection("TransitDb"), true);

        }
    }
}