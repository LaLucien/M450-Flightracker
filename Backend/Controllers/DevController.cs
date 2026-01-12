using Microsoft.AspNetCore.Mvc;
using FlightTracker.Api.Services;

namespace FlightTracker.Api.Controllers
{
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly DataSeederService _seederService;

        public DevController(DataSeederService seederService)
        {
            _seederService = seederService;
        }

        [HttpPost("seed")]
        public IActionResult SeedData()
        {
            try
            {
                _seederService.SeedSampleData();
                return Ok(new { message = "Sample data seeded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
