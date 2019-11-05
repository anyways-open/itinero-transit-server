using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Itinero.Transit.Api.Logic.Search
{
    public class SmallTrie<T>
    {
        private readonly Dictionary<char, SmallTrie<T>> _children = new Dictionary<char, SmallTrie<T>>();
        private T _value;

        public void Add(string key, T value)
        {
            Add(key.ToCharArray().ToList(), value);
        }
        
        public void Add(List<char> key, T value)
        {
            if (key.Count == 0)
            {
                _value = value;
                return;
            }

            var firstChar = key[0];
            if (!_children.ContainsKey(firstChar))
            {
                _children[firstChar] = new SmallTrie<T>();
            }

            var subKey = key.GetRange(1, key.Count - 1);
            _children[firstChar].Add(subKey, value);
        }

        [Pure]
        public T Find(string key)
        {
            return Find(key.ToCharArray().ToList());
        }

        [Pure]
        public T Find(List<char> key)
        {
            if (key.Count == 0)
            {
                return _value;
            }

            var firstChar = key[0];


            if (!_children.ContainsKey(firstChar))
            {
                return default;
            }

            var subKey = key.GetRange(1, key.Count - 1);
            return _children[firstChar].Find(subKey);
        }

        [Pure]
        public IEnumerable<(T, int)> FindFuzzy(List<char> key, int maxDistance)
        {
            var results = new HashSet<(T, int)>();
            FindFuzzy(key, results, maxDistance, maxDistance);
            return results;
        }

        private void FindFuzzy(List<char> key, ICollection<(T, int)> results, int startDistance, int maxDistance)
        {
            if (key.Count == 0)
            {
                // Adds the value and child values
                AddPrefixes(results, startDistance-maxDistance);
                return;
            }

            var firstChar = key[0];


            var subKey = key.GetRange(1, key.Count - 1);
            if (!_children.ContainsKey(firstChar))
            {
                if (maxDistance <= 0)
                {
                    return;
                }

                foreach (var child in _children)
                {
                    child.Value.FindFuzzy(key, results, startDistance, maxDistance - 1);
                    child.Value.FindFuzzy(subKey, results, startDistance, maxDistance - 1);
                }
            }
            else
            {
                _children[firstChar].FindFuzzy(subKey, results, startDistance, maxDistance);
            }
        }

        [Pure]
        public IEnumerable<(T, int)> FindFuzzy(string query, int maxDistance)
        {
            return FindFuzzy(query.ToCharArray().ToList(), maxDistance);
        }


        private void AddPrefixes(ICollection<(T, int)> addTo, int distance)
        {
            if (_value != null)
            {
                addTo.Add((_value, distance));
            }

            foreach (var child in _children)
            {
                child.Value.AddPrefixes(addTo, distance + 1);
            }
        }
    }
}