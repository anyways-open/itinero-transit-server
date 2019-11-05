using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
using Itinero.Transit.Data.Core;
using Itinero.Transit.Utils;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    ///  The operator Manager keeps track of all the loaded providers/operators.
    ///
    /// It is able to generate and cache a 'View' of the loaded operators. This 'View' is the central point that all the algorithms use
    /// 
    /// </summary>
    public class OperatorManager
    {
        public readonly List<Operator> All;
        private readonly Dictionary<string, Operator> _operatorsByName = new Dictionary<string, Operator>();
        private readonly Dictionary<string, List<Operator>> _operatorsByTags = new Dictionary<string, List<Operator>>();

        public OperatorManager(IEnumerable<Operator> operators)
        {
            All = operators.OrderBy(op => op.Tdb.DatabaseId).ToList();

            foreach (var @operator in All)
            {
                _operatorsByName[@operator.Name.ToLower()] = @operator;
                foreach (var altName in @operator.AltNames)
                {
                    _operatorsByName[altName.ToLower()] = @operator;
                }

                foreach (var tag in @operator.Tags)
                {
                    if (!_operatorsByTags.ContainsKey(tag))
                    {
                        _operatorsByTags[tag.ToLower()] = new List<Operator>();
                    }

                    _operatorsByTags[tag.ToLower()].Add(@operator);
                }
            }
        }

        private Dictionary<string, OperatorSet> _cachedViews = new Dictionary<string, OperatorSet>();

        private OperatorSet GetView(List<Operator> operators)
        {
            if (operators == null)
            {
                throw new NullReferenceException("If you want an operator set, pass in a list of operators");
            }

            var names = operators.Select(op => op.Name.ToLower()).ToList();
            names.Sort();
            var cacheKey = string.Join("\n", names);

            if (_cachedViews.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var newSet = new OperatorSet(operators);
            _cachedViews.Add(cacheKey, newSet);
            return newSet;
        }


        public OperatorSet GetFullView()
        {
            return GetView(All);
        }

        public OperatorSet GetView(string namesOrTags)
        {
            if (namesOrTags.Equals("*"))
            {
                return GetFullView();
            }

            return GetView(namesOrTags.ToLower().Split(";"));
        }


        private OperatorSet GetView(IEnumerable<string> namesAndTags)
        {
            var results = new HashSet<Operator>();
            foreach (var nameOrTag in namesAndTags)
            {
                if (_operatorsByName.TryGetValue(nameOrTag, out var @operator))
                {
                    results.Add(@operator);
                }

                if (_operatorsByTags.TryGetValue(nameOrTag, out var operators))
                {
                    foreach (var op in operators)
                    {
                        results.Add(op);
                    }
                }
            }

            return GetView(results.ToList());
        }
    }

    public class OperatorSet
    {
        /// <summary>
        /// This dictionary contains all the loaded transitDbs, indexed on their name.
        /// The object responsible of regularly reloading them is included too
        /// </summary>
        public readonly List<Operator> Operators;

        public IEnumerable<TransitDbSnapShot> All()
        {
            if (Operators == null)
            {
                return new List<TransitDbSnapShot>();
            }

            return Operators.Select(v => v.Tdb.Latest);
        }

        private StopSearchCache _cachedStopsReader;

        public OperatorSet(List<Operator> transitDbs)
        {
            Operators = transitDbs;
        }

        /// <summary>
        /// Get a stops reader for all the loaded databases.
        /// </summary>
        /// <returns></returns>
        public IStopsReader GetStopsReader()
        {
            var newReader = StopsReaderAggregator.CreateFrom(
                All().Select(tdb => (IStopsReader) tdb.StopsDb.GetReader()).ToList());
            if (_cachedStopsReader == null)
            {
                _cachedStopsReader = newReader.UseCache();
                return _cachedStopsReader;
            }

            // Return a new stospReader which shares the cache with the already existing cache
            return new StopSearchCache(newReader, _cachedStopsReader);
        }

        public IDatabaseReader<ConnectionId, Connection> GetConnectionsReader()
        {
            var readers = All().Select(tdb => tdb.ConnectionsDb);

            return DatabaseEnumeratorAggregator<ConnectionId, Connection>.CreateFrom(readers);
        }

        public IConnectionEnumerator GetConnections()
        {
            var readers = All().Select(tdb =>
                (IConnectionEnumerator) tdb.ConnectionsDb.GetDepartureEnumerator()).ToList();

            return ConnectionEnumeratorAggregator.CreateFrom(readers);
        }


        public IDatabaseReader<TripId, Trip> GetTripsReader()
        {
            var readers =
                All().Select(tdb => tdb.TripsDb);
            return DatabaseEnumeratorAggregator<TripId, Trip>.CreateFrom(readers);
        }

        public DateTime EarliestLoadedTime()
        {
            return All().Select(tdb => tdb.ConnectionsDb.EarliestDate).Max().FromUnixTime();
        }

        public DateTime LatestLoadedTime()
        {
            return All().Select(tdb => tdb.ConnectionsDb.LatestDate).Min().FromUnixTime();
        }
    }
}