using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Performs flight scraping operations
/// </summary>
public interface IFlightScrapingService
{
    Task ScrapeFlightsAsync(CancellationToken cancellationToken = default);
    Task Scrape(QueryEntity query, CancellationToken cancellationToken);
}
