using FlightTracker.Contracts;

namespace FlightTracker.Api.Services.Selenium
{
    public interface IFlightScraper
    {
        Task ScrapeFlights();
    }
}
