using FlightTracker.Api.Services.Selenium;
using FlightTracker.Contracts;

namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Default implementation that scrapes predefined flight routes
/// TODO: Implement
/// </summary>
public class DefaultFlightScrapingService : IFlightScrapingService
{
    private readonly FlightCollectionService _collectionService;

    public DefaultFlightScrapingService(FlightCollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public async Task ScrapeFlightsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("implement scraping flight routes");
    }
}
