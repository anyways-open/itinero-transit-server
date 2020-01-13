using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using Itinero.Transit.Data;
using Itinero.Transit.Data.Core;
using Serilog;

namespace Itinero.Transit.Api.Logic.Search
{
    /// <summary>
    /// Builds a suffix tree of all the names of the stopsReader
    /// </summary>
    public class NameIndexBuilder
    {
        private List<string> _attributeKeysToUse;


        public NameIndexBuilder(List<string> attributeKeysToUse)
        {
            _attributeKeysToUse = attributeKeysToUse;
        }


        public NameIndex Build(IStopsDb reader)
        {
            return new NameIndex(BuildTrie(reader), reader);
        }

        public SmallTrie<(string, int)> BuildTrie(IStopsDb allStops)
        {
            var trie = new SmallTrie<(string, int)>();

            foreach (var stop in allStops)
            {
                var names = ExtractNames(stop);
                if (names == null)
                {
                    continue;
                }

                foreach (var (name, dist) in names)
                {
                    if (name == null)
                    {
                        continue;
                    }

                    trie.Add(name.ToCharArray().ToList(), (stop.GlobalId, dist));
                }
            }

            return trie;
        }


        private IEnumerable<(string, int)> ExtractNames(Stop stop)
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
                (Simplify(name), 0),
                (Initials(name), 1),
                (Initials2(name), 1)
            };
            SaintInitials(results, name);
            return results.Where(v => v.Item1 != null);
        }

        private static string Initials(string s)
        {
            var initialsRegex = new Regex(@"(\b[a-zA-Z])[a-zA-Z]*[^a-z]?");

            var initials = initialsRegex.Replace(s, "$1");
            return initials;
        }

        private static string Initials2(string s)
        {
            if (s.Length <= 2 || !s.Contains(' '))
            {
                return null;
            }

            return s.Substring(0, 2) + Initials(s).Substring(1);
        }

        private static void SaintInitials(ICollection<(string, int)> addTo, string fullName)
        {
            if (fullName.StartsWith("sint "))
            {
                addTo.Add(("st" + fullName.Substring(5), 1));
                return;
            }

            if (fullName.StartsWith("sint"))
            {
                addTo.Add(("st" + fullName.Substring(4), 1));
            }
        }


        private static string Clean(string v)
        {
            return v.ToLower().Normalize().Replace(".", "");
        }


        private string NameByAttribute(Stop stop)
        {
            try
            {
                var attr = stop.Attributes;
                foreach (var key in _attributeKeysToUse)
                {
                    if (attr.TryGetValue(key, out var v))
                    {
                        return v;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while determining name: {e}");
                return null;
            }

            Log.Warning($"No name found for {stop.GlobalId}");
            return null;
        }

        [Pure]
        public static string Simplify(string s)
        {
            s = s.ToLower();
            s = Regex.Replace(s, @"[^a-z]", "");
            return s;
        }
    }
}