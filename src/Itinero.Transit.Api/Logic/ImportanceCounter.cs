using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    public class ImportanceCounter : SynchronizationPolicy
    {
        public uint Frequency { get; }

        private string _state;
        private ulong nowScanning = 0;

        public ImportanceCounter(uint frequency = 24*60*60)
        {
            Frequency = frequency;
            _state = "Initialized, not running";
        }

        public void Run(DateTime triggerDate, TransitDbUpdater db)
        {
            var frequencies = new Dictionary<(uint, uint), uint>();

            // Count how many connections depart at the given station

            _state = "Scanning connections, currently at:h ";
            var enumerator = db.TransitDb.Latest.ConnectionsDb.GetDepartureEnumerator();
            foreach (var (start, end) in db.LoadedTimeWindows)
            {
                enumerator.MoveNext(start);
                while (enumerator.DepartureTime < end.ToUnixTime())
                {
                    nowScanning = enumerator.DepartureTime;
                    IncreaseCount(frequencies, enumerator.DepartureStop);
                    IncreaseCount(frequencies, enumerator.ArrivalStop);
                    enumerator.MoveNext();
                }
            }

            _state = "Converting internal IDs into URIs";
            nowScanning = 0;
            var stopsReader = db.TransitDb.Latest.StopsDb.GetReader();
            var importances = new Dictionary<string, uint>();
            // Translate internal ids to URI's
            foreach (var (id, importance) in frequencies)
            {
                stopsReader.MoveTo(id);
                importances[stopsReader.GlobalId] = importance;
            }

            State.Importances = importances;
            _state = "Done";
        }


        // ReSharper disable once InconsistentNaming
        private static void IncreaseCount<K>(IDictionary<K, uint> dict, K key)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, 1);
            }
            else
            {
                dict[key] += 1;
            }
        }

        public override string ToString()
        {
            var date = nowScanning == 0 ? "" : $"{nowScanning.FromUnixTime():O}";
            return $"Importance counter. {_state} {date}";
        }
    }
}