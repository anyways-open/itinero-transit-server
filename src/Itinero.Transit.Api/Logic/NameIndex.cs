using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;

namespace Itinero.Transit.Api.Logic
{
    public class NameIndex
    {
        private SmallTrie<(string, int)> _index;
        public IStopsReader StopsReader;

        public NameIndex(SmallTrie<(string, int)> index,
            IStopsReader stopsReader)
        {
            _index = index;
            StopsReader = stopsReader;
        }

        public List<LocationResult> Match(string query)
        {
            query = NameIndexBuilder.Simplify(query);
            var finds = _index.FindFuzzy(query, 10);


            var results = new List<LocationResult>();
            foreach (var ((locationUrl, isActualName), levDistance) in finds)
            {
                if (locationUrl == null)
                {
                    continue;
                }

                // ActualName is '0' if no difference with the actual name exists
                var difference = 2 * isActualName + levDistance;
                var importance =
                    State.Importances != null && State.Importances.ContainsKey(locationUrl)
                        ? State.Importances[locationUrl]
                        : 0;

                StopsReader.MoveTo(locationUrl);

                var location = new Location(StopsReader);
                var locationResult = new LocationResult(
                    location, difference, importance
                );
                results.Add(locationResult);
            }

            results = results.OrderBy(lr => lr.Difference).ThenBy(lr => -lr.Importance).ToList();

            if (results.Count > 10)
            {
                results = results.GetRange(0, 10);
            }

            return results;
        }


  
    }
}