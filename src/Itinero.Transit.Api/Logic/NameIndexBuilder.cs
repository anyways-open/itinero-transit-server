using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Itinero.Transit.Data;

namespace Itinero.Transit.Api.Logic
{
    /// <summary>
    /// Builds a suffix tree of all the names of the stopsReader
    /// </summary>
    public class NameIndexBuilder
    {
        private Reminiscence.Collections.List<string> _attributeKeysToUse;
        

        public NameIndexBuilder(Reminiscence.Collections.List<string> attributeKeysToUse)
        {
            _attributeKeysToUse = attributeKeysToUse;
        }


        public NameIndex Build(IStopsReader reader)
        {
            return new NameIndex(BuildTrie(reader), reader);
        }

        public SmallTrie<(string, int)> BuildTrie(IStopsReader allStops)
        {
            var trie = new SmallTrie<(string, int)>();
            allStops.Reset();
            while (allStops.MoveNext())
            {
                var names = ExtractNames(allStops);
                foreach (var (name, dist) in names)
                {
                    trie.Add(name.ToCharArray().ToList(), (allStops.GlobalId, dist));
                }
            }

            return trie;
        }


        private IEnumerable<(string, int)> ExtractNames(IStop stop)
        {
            var n = NameByAttribute(stop);
            if (n == null)
            {
                return null;
            }

            n = Clean(n);
            return CreateShortenedNames(n);
        }


        private static IEnumerable<(string, int)> CreateShortenedNames(string name)
        {
            var results = new List<(string, int)>
            {
                (name, 0),
                (Initials(name), 1),
                (Initials2(name), 1)
            };
            SaintInitials(results, name);
            return results;
        }

        private static string Initials(string s)
        {
            var initials = new Regex(@"(\b[a-zA-Z])[a-zA-Z]* ?");

            return initials.Replace(s, "$1");
        }

        private static string Initials2(string s)
        {
            return s.Substring(0, 2) + Initials(s).Substring(1);
        }

        private static void SaintInitials(List<(string, int)> addTo, string fullName)
        {
            if (fullName.StartsWith("sint "))
            {
                addTo.Add(("st" + fullName.Substring(5), 1));
                return;
            }

            if (fullName.StartsWith("sint"))
            {
                addTo.Add(("st" + fullName.Substring(4),1));
            }
        }


        private static string Clean(string v)
        {
            return v.ToLower().Normalize().Replace(".", "");
        }


        private string NameByAttribute(IStop stop)
        {
            var attr = stop.Attributes;
            foreach (var key in _attributeKeysToUse)
            {
                if (attr.TryGetValue(key, out var v))
                {
                    return v;
                }
            }

            return null;
        }
    }
}