using FlightTracker.Contracts;

namespace FlightTracker.Api.Services
{
    public interface IFlightService
    {
        Task<List<FlightDto>> GetFlightsAsync();
    }
}
