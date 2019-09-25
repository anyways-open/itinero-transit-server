using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Itinero.Transit.Api.Logging;
using Xunit;

namespace Itinero.Transit.API.Tests
{
    public class LoggerTest
    {
        [Fact]
        public void WriteLogEntry_Test()
        {
            var start = DateTime.Now;
            var logger = new FileLogger("test");
            logger.WriteLogEntry("cat", new Dictionary<string, string>()
            {
                {"foo","bar"}
            });
            var end = DateTime.Now;
            Assert.True((end - start).TotalMilliseconds < 10);
            Thread.Sleep(1100);
        }

        [Fact]
        public void WriteLogEntries_TestIsWritten()
        {
            var logger = new FileLogger("test");
            File.Delete(logger.ConstructPath("cat"));
            logger.WriteLogEntry("cat", new Dictionary<string, string>
            {
                {"foo","bar"}
            });
            logger.WriteLogEntry("cat", new Dictionary<string, string>
            {
                {"abc","def"}
            });
            Thread.Sleep(1500);
            var read = File.ReadAllLines(logger.ConstructPath("cat"));
            Assert.Equal(2, read.Length);
            
            Assert.True(read[0].Contains("{\"foo\":\"bar\"") || read[1].Contains("{\"foo\":\"bar\""));
            Assert.True(read[0].Contains("{\"abc\":\"def\"") || read[1].Contains("{\"abc\":\"def\""));

        }

        [Fact]
        public void ConstructPath_ExpectsPath()
        {
            var logger = new FileLogger("test");
            var path = logger.ConstructPath("cat");
            Assert.Equal($"test/cat-{DateTime.Now:yyyy-MM-dd}.log", path);
        }
    }
}