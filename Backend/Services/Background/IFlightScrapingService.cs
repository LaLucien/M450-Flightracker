namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Performs flight scraping operations
/// </summary>
public interface IFlightScrapingService
{
    Task ScrapeFlightsAsync(CancellationToken cancellationToken = default);
}
