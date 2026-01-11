using FlightTracker.Contracts;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Api.Services;
using System.Globalization;

namespace FlightTracker.Api.Controllers;

[ApiController]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly FlightRepository _flightRepository;
    private readonly ObservationRepository _observationRepository;
    private readonly FlightStatsService _statsService;

    public RoutesController(
        FlightRepository flightRepository,
        ObservationRepository observationRepository,
        FlightStatsService statsService)
    {
        _flightRepository = flightRepository;
        _observationRepository = observationRepository;
        _statsService = statsService;
    }

    [HttpGet("{origin}/{destination}/stats/flex")]
    public ActionResult<FlexStatsResponseDto> GetFlexStats(
        string origin,
        string destination,
        [FromQuery] string target_date,
        [FromQuery] int flex_days)
    {
        if (string.IsNullOrWhiteSpace(target_date))
        {
            return BadRequest("target_date is required");
        }

        if (!DateTime.TryParseExact(target_date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetDate))
        {
            return BadRequest("Invalid target_date format. Use YYYY-MM-DD.");
        }

        if (flex_days < 0)
        {
            return BadRequest("flex_days must be non-negative");
        }

        var startDate = targetDate.AddDays(-flex_days);
        var endDate = targetDate.AddDays(flex_days);

        var series = new List<FlexStatsBucketDto>();

        // For each date in the flex window
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            // Find all flights matching origin/destination and this departure date
            var flights = _flightRepository.Query(origin, destination, date, null);

            if (flights.Count == 0)
            {
                // No flights for this date, skip
                continue;
            }

            // For each flight, compute median price across all observations
            var flightMedians = flights.Select(flight =>
            {
                var observations = _observationRepository.GetByFlightId(flight.Id.ToString());
                var prices = observations.Select(o => o.PriceChf).ToList();
                var median = _statsService.Median(prices);

                return new
                {
                    Flight = flight,
                    Median = median
                };
            }).Where(x => x.Median.HasValue).ToList();

            if (flightMedians.Count == 0)
            {
                // No observations for any flight on this date
                continue;
            }

            // Choose the flight with the lowest median
            var bestForDate = flightMedians.OrderBy(x => x.Median!.Value).First();

            series.Add(new FlexStatsBucketDto
            {
                DepartureDate = DateOnly.FromDateTime(date).ToString("yyyy-MM-dd"),
                MedianPriceChf = bestForDate.Median!.Value,
                FlightId = bestForDate.Flight.Id.ToString()
            });
        }

        // Find the overall best (minimum median in the entire window)
        FlexStatsBucketDto? best = null;
        if (series.Count > 0)
        {
            best = series.OrderBy(s => s.MedianPriceChf).First();
        }

        return Ok(new FlexStatsResponseDto
        {
            Origin = origin,
            Destination = destination,
            TargetDate = targetDate.ToString("yyyy-MM-dd"),
            FlexDays = flex_days,
            Timezone = "Europe/Zurich",
            Series = series.OrderBy(s => s.DepartureDate).ToList(),
            Best = best
        });
    }
}
