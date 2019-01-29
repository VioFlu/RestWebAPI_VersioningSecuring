using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace MyCodeCamp.Controllers
{
    [Route("api/[controller]")]
    public class OperationsController : Controller
    {
        private ILogger<OperationsController> _logger;
        private readonly IConfigurationRoot _config;

        public OperationsController(ILogger<OperationsController> logger,
                                    IConfigurationRoot config)
        {
            _logger = logger;
            _config = config;
        }
        [HttpOptions("reloadConfig")]
        public IActionResult ReloadConfiguration()
        {
            try
            {
                _config.Reload();
                return Ok("Configuration reloaded");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception ");
            }
            return BadRequest("Could not reload configuration");

        }
    }
}
