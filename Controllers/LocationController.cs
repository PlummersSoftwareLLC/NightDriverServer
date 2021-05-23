using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace WebHost.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILogger<LocationController> _logger;
        public LocationController(ILogger<LocationController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<NightDriver.Location> GetLocations()
        {
            return NightDriver.ConsoleApp.Locations;
        }

        [HttpGet("{id:int}")]
        public NightDriver.Location GetLocation(int id)
        {
            if (id < 0 || id > NightDriver.ConsoleApp.Locations.Length)
                throw new ArgumentOutOfRangeException("id");

            return NightDriver.ConsoleApp.Locations[id];
        }

    }
}
