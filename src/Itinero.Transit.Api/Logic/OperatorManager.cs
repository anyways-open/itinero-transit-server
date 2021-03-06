using System;
using System.Collections.Generic;
using System.Linq;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Aggregators;
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

            for (var i = 0; i < All.Count; i++)
            {
                if (i != All[i].Tdb.DatabaseId)
                {
                    throw new Exception(
                        "PANIC: incorrect databaseID: either duplicates are gaps are passed into the operator manager");
                }
            }

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

        public OperatorSet(List<Operator> transitDbs)
        {
            Operators = transitDbs;
        }


        private IStopsDb _cachedStopsDb;

        public IStopsDb GetStops()
        {
            if (_cachedStopsDb == null)
            {
                _cachedStopsDb = StopsDbAggregator.CreateFrom(
                    All().Select(tdb => tdb.StopsDb).ToList()).UseCache();
            }

            return _cachedStopsDb;
        }

        public IConnectionsDb GetConnections()
        {
            var dbs = All().Select(tdb => tdb.ConnectionsDb);
            return ConnectionsDbAggregator.CreateFrom(dbs.ToList());
        }


        public ITripsDb GetTrips()
        {
            var dbs =
                All().Select(tdb => tdb.TripsDb);
            return TripsDbAggregator.CreateFrom(dbs.ToList());
        }

        public DateTime EarliestLoadedTime()
        {
            var unixTime = All().Select(tdb => tdb.ConnectionsDb.EarliestDate).Max();
            if (unixTime == ulong.MaxValue || unixTime == 0)
            {
                return DateTime.MaxValue;
            }

            return unixTime.FromUnixTime();
        }

        public DateTime LatestLoadedTime()
        {
            var unixTime = All().Select(tdb => tdb.ConnectionsDb.LatestDate).Min();
            if (unixTime == ulong.MaxValue || unixTime == 0)
            {
                return DateTime.MaxValue;
            }

            return unixTime.FromUnixTime();
        }
    }
}