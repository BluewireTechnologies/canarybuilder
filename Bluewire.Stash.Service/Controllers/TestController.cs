using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bluewire.Stash.Service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Test.";
        }
    }
}
