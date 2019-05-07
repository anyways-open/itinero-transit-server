using System.Collections.Generic;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class LocationsByNameController : ControllerBase
    {
        /// <summary>
        /// Searches for stops having the given name or something similar.
        /// Includes an 'importance'-score for each station.
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
        ///
        ///
        /// The importance-score is based on how many trains arrive/depart in the given station.
        /// This is calculated using the connections which are stored in the database.
        /// Note that the importance scores are _not_ available directly after booting the server. It takes a few minutes, until the entire server has been refreshed.
        /// Up till that point, all stations are scored at 0.
        /// </remarks>
        [HttpGet]
        public ActionResult<List<LocationResult>> Get(string name)
        {
            var matches = State.NameIndex.Match(name);
            
            if (matches.Count == 0)
            {
                return NotFound($"No stations found for search string {name}");
            }

            return matches;
        }
    }
}