using System;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class GeoJsonJourneyController : ControllerBase
    {
        /// <summary>
        /// Creates a journey over the public-transport network.
        /// Returns the result as geojson.
        /// This is identical to the Journey-endpoint. Please read the documentation over there
        /// </summary>
        [HttpGet]
        public ActionResult<Geojson> Get(
            string from,
            string to,
            string walksGeneratorDescription = JourneyController.DefaultWalkGenerator,
            string inBetweenOsmProfile = null,
            uint inBetweenSearchDistance = 1500,
            string firstMileOsmProfile = null,
            uint firstMileSearchDistance = 1500,
            string lastMileOsmProfile = null,
            uint lastMileSearchDistance = 1500,
            DateTime? departure = null,
            DateTime? arrival = null,
            uint internalTransferTime = 180,
            uint maxNumberOfTransfers = 4,
            bool prune = true
        )
        {
            var controller = new JourneyController();
            var response = controller.Get(
                from,
                to,
                walksGeneratorDescription,
                inBetweenOsmProfile,
                inBetweenSearchDistance,
                firstMileOsmProfile,
                firstMileSearchDistance,
                lastMileOsmProfile,
                lastMileSearchDistance,
                departure,
                arrival,
                internalTransferTime,
                maxNumberOfTransfers,
                prune
            );
            if (response.Value != null)
            {
                return response.Value.Journeys[0].AsGeoJson();
            }

            return BadRequest("Something went wrong. Check the parameters");
        }
    }
}