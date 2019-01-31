using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;
using static System.String;
using Location = Itinero.Transit.IO.LC.CSA.LocationProviders.Location;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public class LocationsByNameController : ControllerBase
    {
       
        /// <summary>
        /// Searches for stops having the given name or something similar. 
        /// </summary>
        /// <remarks>
        /// A match is calculated as following:
        /// First, all characters are lowercased and non [a-z]-characters are removed.
        /// 0) Then, we search for an exact match (which will get a 'difference' score of **0**).
        /// 1) Secondly, a number of acronyms are automatically calculated for each station, namely:
        ///    The initials (e.g. Gent-Sint-Pieter will be shortened to GSP;)
        ///    The first two letters, followed by initials (Brussel-Centraal becomes BrC).
        ///     These are returned with a difference number of **1**.
        /// 2) If the station name starts with the requested query, it is returned with a value of **2**
        /// 3) At last, stations are matched with a string distance function. If the string distance is smaller then 5, it is returned.
        ///    The difference will be the string comparison difference + 1
        /// </remarks>
        [HttpGet]
        public ActionResult<List<LocationResult>> Get(string name)
        {
            const int maxDistance = 15;

            var results = new List<List<Location>>();

            for (var i = 0; i <= maxDistance + 2; i++)
            {
                results.Add(new List<Location>());
            }

            name = Simplify(name);

            foreach (var lp in State.LcProfile.LocationProvider)
            {
                foreach (var l in lp.Locations)
                {
                    var lName = Simplify(l.Name);

                    if (Equals(lName, name))
                    {
                        results[0].Add(l);
                        continue;
                    }

                    if (name.Length > 2 &&
                        name.Equals(Initials(l.Name))
                        || name.Equals(Initials2(l.Name))
                        || SaintInitials(lName, name))
                    {
                        results[1].Add(l);
                        continue;
                    }


                    if (lName.StartsWith(name))
                    {
                        results[2].Add(l);
                        continue;
                    }

                    var d = CalcLevenshteinDistance(name, lName.Substring(0, Math.Min(lName.Length, name.Length)));
                    if (d <= maxDistance)
                    {
                        results[d + 2].Add(l);
                    }
                }
            }

            var json = new List<LocationResult>();
            for (var i = 0; i <= maxDistance + 2; i++)
            {
                foreach (var r in results[i])
                {
                    var id = r.Uri.ToString();
                    uint importance = 0;
                    if (State.Importances != null && State.Importances.ContainsKey(id))
                    {
                        importance = State.Importances[id];
                    }

                    var loc = new Models.Location(r.Id().ToString(), r.Name, r.Lat, r.Lon);
                    json.Add(new LocationResult(loc, i, importance));
                }
            }

            if (json.Count == 0)
            {
                return NotFound($"No stations found for search string {name}");
            }

            // Forgive me, Ben, for I have sinned by using Linq
            return json.OrderBy(o => o.Difference).ThenByDescending(o => o.Importance).ToList();
        }

        private static string Simplify(string s)
        {
            s = s.ToLower();
            s = Regex.Replace(s, @"[^a-z]", "");
            return s;
        }

        private static string Initials(string s)
        {
            var initials = new Regex(@"(\b[a-zA-Z])[a-zA-Z]* ?");

            return Simplify(initials.Replace(s, "$1"));
        }

        private static string Initials2(string s)
        {
            return Simplify(s.Substring(0, 2) + Initials(s).Substring(1));
        }

        private static bool SaintInitials(string fullName, string query)
        {
            if (!fullName.StartsWith("sint"))
            {
                return false;
            }

            return query.StartsWith("st") && fullName.Substring(4).StartsWith(query.Substring(2));
        }


        private static int CalcLevenshteinDistance(string a, string b)
        {
            // Comes from stack overflow
            if (IsNullOrEmpty(a) && IsNullOrEmpty(b))
            {
                return 0;
            }

            if (IsNullOrEmpty(a))
            {
                return b.Length;
            }

            if (IsNullOrEmpty(b))
            {
                return a.Length;
            }

            var lengthA = a.Length;
            var lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (var i = 0; i <= lengthA; distances[i, 0] = i++) ;
            for (var j = 0; j <= lengthB; distances[0, j] = j++) ;

            for (var i = 1; i <= lengthA; i++)
            for (var j = 1; j <= lengthB; j++)
            {
                var cost = b[j - 1] == a[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min
                (
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost
                );
            }

            return distances[lengthA, lengthB];
        }
    }
}