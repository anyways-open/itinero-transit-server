using System;
using System.Collections.Generic;
using Itinero.Transit.IO.LC.CSA;
using Itinero.Transit.IO.LC.CSA.ConnectionProviders;
using Itinero.Transit.IO.LC.CSA.LocationProviders;

namespace Itinero.Transit.Api.Logic
{
    public static class ImportanceCount
    {
        /// <summary>
        /// Calculates how important a certain station is based on how many trains stop there
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, uint> CalculateImportance(LinkedConnectionDataset profile, DateTime start, DateTime end)
        {
            var importances = new Dictionary<string, uint>();

            for (var i = 0; i < profile.ConnectionsProvider.Count; i++)
            {
                AddEntries(importances, profile.LocationProvider[i], profile.ConnectionsProvider[i], start, end);
            }

            return importances;
        }

        private static void AddEntries(this IDictionary<string, uint> d, LocationProvider locP, ConnectionProvider conP,
            DateTime start, DateTime end)
        {
            var tt = conP.GetTimeTable(start);

            while (tt.StartTime() < end)
            {
                foreach (var connection in tt.Connections())
                {
                    d.Inc(connection.DepartureLocation());
                    d.Inc(connection.ArrivalLocation());
                }

                tt = conP.GetTimeTable(tt.NextTable());
            }
        }

        private static void Inc(this IDictionary<string, uint> d, Uri keyUri)
        {
            var key = keyUri.ToString();

            if (d.ContainsKey(key))
            {
                d[key]++;
            }
            else
            {
                d.Add(key, 1);
            }
        }
    }
}