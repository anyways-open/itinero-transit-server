using System;
using System.Globalization;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        /// <summary>
        /// The controller which calculates station-to-station journeys.
        /// </summary>
        /// <param name="from">The departure station where the traveller would like to depart. Format is a station URI, such as http://irail.be/stations/NMBS/008891009</param>
        /// <param name="to">The station where the traveller would like to go. Format: see from</param>
        /// <param name="date">The date when the traveller would like to travel. Defaults to today (server time)</param>
        /// <param name="time">The moment in time when the traveller would like to travel. Defaults to now (server time)</param>
        /// <param name="timeSel">The interpretation of the date: does the traveller want to depart or arrive at the specified time? Either "depart" or "arrive". Defaults to "depart"</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<string> Get(string from, string to,
            string date, string time, string timeSel = "depart")
        {
            // DO NOT CHANGE THE PARAMETER NAMES!
            // The should mach the URL-parameters EXACTLY


            var router = PublicTransportRouter.BelgiumSncb;

            // -------------------------------- Check input ---------------------

            timeSel = timeSel.ToLower().Trim();
            if (!(Equals("depart", timeSel) || Equals("arrive", timeSel)))
            {
                return BadRequest("TimeSel is not valid, should be either 'depart' or 'arrive'");
            }

            var departureStopId = router.GetStop(from);
            var arrivalStopId = router.GetStop(to);

            if (departureStopId == arrivalStopId)
            {
                return BadRequest("The departure station is the same as the arrival station. You're not gonna get far this way");
            }

            // ------------------------------- Configure dates ------------------
            if (string.IsNullOrEmpty(date))
            {
                date = $"{DateTime.Today:ddMMyy}";
            }

            if (string.IsNullOrEmpty(time))
            {
                time = $"{DateTime.Now:HHmm}";
            }

            if (!DateTime.TryParseExact($"{date} {time}", "ddMMyy HHmm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var moment))
            {
                return BadRequest("Invalid date or time. Format should be 'date=DDMMYY', 'time=HHMM'");
            }

            var response = PublicTransportRouter.BelgiumSncb.EarliestArrivalRoute(
                departureStopId, arrivalStopId, moment, moment.AddHours(24));
            
            return new JsonResult(response);
        }
    }
}