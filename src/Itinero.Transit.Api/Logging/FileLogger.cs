using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Itinero.Transit.Api.Logic;

[assembly: InternalsVisibleTo("Itinero.Transit.API.Tests")]

namespace Itinero.Transit.Api.Logging
{
    public class FileLogger
    {
        private readonly string _directory;

        private readonly Dictionary<string, (StreamWriter, DateTime createdDate)> _writers =
            new Dictionary<string, (StreamWriter, DateTime createdDate)>();

        public FileLogger(string directory)
        {
            _directory = directory;
        }

        public void WriteLogEntry(string category, Dictionary<string, string> toLog)
        {
            Task.Run(() => WriteLogEntryAsync(category, toLog));
        }


        private void WriteLogEntryAsync(string category, Dictionary<string, string> toLog)
        {
            var path = ConstructPath(category);
            toLog.Add("timestamp", DateTime.Now.ToString("s"));
            toLog.Add("api-version", State.VersionNr);
            var data = string.Join(",",
                toLog.Select((kv, _) => "{" + $"\"{kv.Key}\":\"{kv.Value}\"" + "}"));

            data = "{" + data + "},";

            var fi = new FileInfo(path);
            if (!Directory.Exists(fi.DirectoryName))
            {
                Directory.CreateDirectory(fi.DirectoryName);
            }


            StreamWriter stream;
            lock (_writers)
            {
                if (!_writers.ContainsKey(path))
                {
                    // We have to create a new writer - this implies that maybe an old writer should be discarded
                    RemoveStaleWriters();
                    // create the new writer
                    _writers[path] = (new StreamWriter(path, true), DateTime.Now);
                }

                (stream, _) = _writers[path];
            }

            lock (stream)
            {
                stream.WriteLine(data);
                stream.Flush();
            }
        }

        private void RemoveStaleWriters()
        {
            var toRemove = new List<string>();
            foreach (var kv in _writers)
            {
                var (writer, createdDate) = kv.Value;
                if (createdDate.AddDays(1) >= DateTime.Now) continue;


                // Older then one day - discard this thing
                writer.Close();
                toRemove.Add(kv.Key);
            }

            foreach (var k in toRemove)
            {
                _writers.Remove(k);
            }
        }

        internal string ConstructPath(string category)
        {
            var date = DateTime.Now.Date;
            return Path.Join(_directory, $"{category}-{date:yyyy-MM-dd}.log");
        }
    }
}