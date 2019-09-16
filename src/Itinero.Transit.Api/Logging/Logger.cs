using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Itinero.Transit.Api.Logic;

[assembly: InternalsVisibleTo("Itinero.Transit.API.Tests")]

namespace Itinero.Transit.Api.Logging
{
    public class FileLogger
    {
        private readonly string _directory;

        public FileLogger(string directory)
        {
            _directory = directory;
        }

        public void WriteLogEntry(string category, Dictionary<string, string> toLog)
        {
            Task.Run(() => WriteLogEntryAsync(category, toLog));
        }


        internal async Task WriteLogEntryAsync(string category, Dictionary<string, string> toLog)
        {
            var path = ConstructPath(category);
            toLog.Add("timestamp", DateTime.Now.ToString("s"));
            toLog.Add("api-version", State.VersionNr);
            var data = string.Join(",",
                toLog.Select((kv, _) => "{"+$"\"{kv.Key}\":\"{kv.Value}\""+"}"));

            data = "{" + data + "},";

            var fi = new FileInfo(path);
            var di = fi.Directory;
            if (!di.Exists)
            {
                Directory.CreateDirectory(di.FullName);
            }

            using (var stream = new StreamWriter(path, true))
            {
                await stream.WriteLineAsync(data);
            }

            Console.WriteLine("Written data! Sleeping now");
        }

        internal string ConstructPath(string category)
        {
            var date = DateTime.Now.Date;
            return Path.Join(_directory, $"{category}-{date:yyyy-MM-dd}.log");
        }
    }
}