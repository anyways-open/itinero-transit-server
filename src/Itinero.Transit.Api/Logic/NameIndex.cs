using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Api.Models;
using Itinero.Transit.Data;

namespace Itinero.Transit.Api.Logic
{
    public class NameIndex
    {
        private readonly SmallTrie<(string, int)> _index;
        private readonly IStopsReader _stopsReader;

        public NameIndex(SmallTrie<(string, int)> index,
            IStopsReader stopsReader)
        {
            _index = index;
            _stopsReader = stopsReader;
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
                    State.GlobalState.Importances != null
                    && State.GlobalState.Importances.ContainsKey(locationUrl)
                        ? State.GlobalState.Importances[locationUrl]
                        : 0;

                _stopsReader.MoveTo(locationUrl);

                var location = new Location(_stopsReader);
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