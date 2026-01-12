
using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Storage.Entities;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Backend.Tests.Services.Background;

public class FlightScraperTest
{
    private readonly IConfiguration _configuration;
    public FlightScraperTest()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets<FlightScraperTest>();
        _configuration = builder.Build();
    }



    [Fact]
    public async Task SmokeTest()
    {
        var flightScraper = new DefaultFlightScrapingService(null, null, _configuration);
        var query = new QueryEntity() { DestinationIata = "ZRH", OriginIata = "JFK", AnchorDate = DateTime.UtcNow.AddDays(30), FlexibilityDays = 2 };
        flightScraper.EnsureGoogleFlightCookiesAccepted();
        var scrapeTask = flightScraper.Scrape(query, CancellationToken.None);
        await scrapeTask;   
    }
}
