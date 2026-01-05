using FlightTracker.Contracts;

namespace FlightTracker.Api.Services.Selenium
{
    public interface IFlightProvider
    {
        Task<List<FlightDto>> FetchFlightsAsync(FlightQueryDto query);
    }
}
