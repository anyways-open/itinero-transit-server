using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Itinero.Transit.Api.Controllers
{
    
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(200)]
    public class StatusController : ControllerBase
    {

        public static StatusReportGenerator Reporter;
        /// <summary>
        /// Gives some insight in the database
        /// </summary>
        [HttpGet]
        public ActionResult<StatusReport> Get()
        {
            return Reporter.CreateReport();
        }
    }
}