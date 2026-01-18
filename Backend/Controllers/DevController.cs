using FlightTracker.Api.Services;
using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Storage.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers
{
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly DataSeederService _seederService;
        private readonly IFlightScrapingService flightScraper;

        public DevController(DataSeederService seederService, IFlightScrapingService scrapingService)
        {
            _seederService = seederService;
            flightScraper = scrapingService;
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

        [HttpGet("DevScrape")]
        public async Task<IActionResult> TestScrape()
        {
            try
            {
                var query = new QueryEntity() { DestinationIata = "ZRH", OriginIata = "JFK", AnchorDate = DateTime.UtcNow.AddDays(30), FlexibilityDays = 2 };
                await flightScraper.Scrape(query, CancellationToken.None);
                return Ok(new { message = "Scraping completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("ManualScrape")]
        public async Task<IActionResult> ScrapeNow()
        {
            try
            {

                await flightScraper.ScrapeFlightsAsync();
                return Ok(new { message = "Success" });
            }
            catch (Exception)
            {
                return StatusCode(500);
            }
        }
    }
}
