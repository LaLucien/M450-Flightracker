using FlightTracker.Api.Services;
using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Storage.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers
{
    [ApiController]
    [Route("api/dev")]
    public class DevController : ControllerBase
    {
        private readonly DataSeederService _seederService;
        private readonly DefaultFlightScrapingService flightScraper;

        public DevController(DataSeederService seederService, DefaultFlightScrapingService scrapingService)
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

        [HttpGet("testScrape")]
        public async Task<IActionResult> TestScrape()
        {
            try
            {
                var query = new QueryEntity() { DestinationIata = "ZRH", OriginIata = "JFK", AnchorDate = DateTime.UtcNow.AddDays(30), FlexibilityDays = 2 };
                flightScraper.EnsureGoogleFlightCookiesAccepted();
                var scrapeTask = flightScraper.Scrape(query, CancellationToken.None);
                await scrapeTask;
                return Ok(new { message = "Scraping completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
