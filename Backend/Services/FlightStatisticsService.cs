using FlightTracker.Contracts;

namespace FlightTracker.Api.Services
{
    public class FlightStatisticsService
    {
        public decimal GetMinPrice(IEnumerable<FlightDto> flights)
        => flights.Min(f => f.Price);

        public decimal GetMaxPrice(IEnumerable<FlightDto> flights)
            => flights.Max(f => f.Price);

        public DayOfWeek GetCheapestWeekday(IEnumerable<FlightDto> flights)
            => flights
                .GroupBy(f => f.DepartureDate.DayOfWeek)
                .OrderBy(g => g.Average(f => f.Price))
                .First()
                .Key;
    }
}
