using System;
using System.IO;
using Itinero.Transit.Api.Logic;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Test
{
    public class ConfigTests
    {
        private string GetRepoPath()
        {
            var dir = Directory.GetCurrentDirectory();
            var i = dir.IndexOf("Test/", StringComparison.Ordinal);
            if (i < 0)
            {
                return dir;
            }
            return dir.Substring(0, i);
        }

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
                try
                {
                    var path = Path.Combine(GetRepoPath(), s);
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile(path);
                    configuration.Build();

                    configuration.Build().CreateTransitDbs(true);
                }
                catch (Exception e)
                {
                    throw new Exception("Exception while testing " + s, e);
                }
            }
        }
    }
}