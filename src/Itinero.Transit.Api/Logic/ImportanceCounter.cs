using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.LocationProviders;
using Itinero.Transit.IO.LC.IO.LC.Synchronization;

namespace Itinero.Transit.Api.Logic
{
    public class ImportanceCounter : SynchronizationPolicy
    {
        public uint Frequency { get; }


        public ImportanceCounter(uint frequency = 24*60*60)
        {
            Frequency = frequency;
        }

        public void Run(DateTime triggerDate, TransitDbUpdater db)
        {
            var frequencies = new Dictionary<(uint, uint), uint>();

            // Count how many connections depart at the given station
            var enumerator = db.TransitDb.Latest.ConnectionsDb.GetDepartureEnumerator();
            foreach (var (start, end) in db.LoadedTimeWindows)
            {
                enumerator.MoveNext(start);
                while (enumerator.DepartureTime < end.ToUnixTime())
                {
                    IncreaseCount(frequencies, enumerator.DepartureStop);
                    IncreaseCount(frequencies, enumerator.ArrivalStop);
                }
            }


            var stopsReader = db.TransitDb.Latest.StopsDb.GetReader();
            var importances = new Dictionary<string, uint>();
            // Translate internal ids to URI's
            foreach (var (id, importance) in frequencies)
            {
                stopsReader.MoveTo(id);
                importances[stopsReader.GlobalId] = importance;
            }


            State.Importances = importances;
        }


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
    }
}