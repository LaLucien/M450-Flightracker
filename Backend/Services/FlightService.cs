using FlightTracker.Contracts;

namespace FlightTracker.Api.Services
{
    public class FlightService : IFlightService
    {
        public Task<List<FlightDto>> GetFlightsAsync()
        {
            return Task.FromResult(new List<FlightDto>
        {
            new FlightDto
            {
                Id = 1,
                Origin = "Zürich",
                Destination = "London",
                Airline = "Swiss",
                DepartureDate = DateTime.Today.AddDays(10),
                Price = 120
            }
        });
        }
    }
}
