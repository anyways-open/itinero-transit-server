using System;
using System.Collections.Generic;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Synchronization;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Api.Logic
{
    public class ImportanceCounter : ISynchronizationPolicy
    {
        public uint Frequency { get; }

        private string _state = "";
        private ulong _nowScanning;

        public ImportanceCounter(uint frequency = 24 * 60 * 60)
        {
            Frequency = frequency;
        }

        public void Run(DateTime triggerDate, TransitDbUpdater db)
        {
            var frequencies = new Dictionary<LocationId, uint>();

            // Count how many connections depart at the given station

            _state = "Scanning connections, currently at: ";
            var enumerator = db.TransitDb.Latest.ConnectionsDb.GetDepartureEnumerator();
            var total = 0;
            foreach (var (start, end) in db.LoadedTimeWindows)
            {
                enumerator.MoveNext(start);
                do
                {
                    _nowScanning = enumerator.DepartureTime;
                    if (enumerator.CanGetOn())
                    {
                        IncreaseCount(frequencies, enumerator.DepartureStop);
                        total++;
                    }

                    if (enumerator.CanGetOff())
                    {
                        IncreaseCount(frequencies, enumerator.ArrivalStop);
                        total++;
                    }
                } while (enumerator.MoveNext() && enumerator.DepartureTime < end.ToUnixTime());
            }

            _state = "Converting internal IDs into URIs";
            _nowScanning = 0;
            var stopsReader = db.TransitDb.Latest.StopsDb.GetReader();
            var importances = new Dictionary<string, uint>();
            // Translate internal ids to URI's
            foreach (var (id, importance) in frequencies)
            {
                stopsReader.MoveTo(id);
                importances[stopsReader.GlobalId] = importance;
            }

            State.GlobalState.ImportancesInternal = frequencies;
            
            
            
            State.GlobalState.Importances = importances;
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
            var date = _nowScanning == 0 ? "" : $"{_nowScanning.FromUnixTime():O}";
            return $"Importance counter. {_state} {date}";
        }
    }
}