using FlightTracker.Contracts;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Services;
using System.Globalization;

namespace FlightTracker.Api.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController : ControllerBase
{
    private readonly FlightRepository _flightRepository;
    private readonly ObservationRepository _observationRepository;
    private readonly FlightStatsService _statsService;

    public FlightsController(
        FlightRepository flightRepository,
        ObservationRepository observationRepository,
        FlightStatsService statsService)
    {
        _flightRepository = flightRepository;
        _observationRepository = observationRepository;
        _statsService = statsService;
    }

    [HttpGet]
    public ActionResult<List<FlightResponseDto>> Get(
        [FromQuery] string? origin = null,
        [FromQuery] string? destination = null,
        [FromQuery] string? departure_date = null,
        [FromQuery] string? flight_number = null)
    {
        DateTime? departureDate = null;
        if (!string.IsNullOrWhiteSpace(departure_date))
        {
            if (!DateTime.TryParseExact(departure_date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return BadRequest("Invalid departure_date format. Use YYYY-MM-DD.");
            }
            departureDate = parsedDate;
        }

        var flights = _flightRepository.Query(origin, destination, departureDate, flight_number);
        var dtos = flights.Select(f => _statsService.ToFlightResponseDto(f)).ToList();
        return Ok(dtos);
    }

    [HttpGet("{flightId}")]
    public ActionResult<FlightResponseDto> GetById(string flightId)
    {
        var flight = _flightRepository.GetById(flightId);
        if (flight == null)
        {
            return NotFound();
        }

        return Ok(_statsService.ToFlightResponseDto(flight));
    }

    [HttpGet("{flightId}/observations")]
    public ActionResult<List<ObservationResponseDto>> GetObservations(
        string flightId,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var flight = _flightRepository.GetById(flightId);
        if (flight == null)
        {
            return NotFound("Flight not found");
        }

        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (!string.IsNullOrWhiteSpace(from))
        {
            if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            {
                return BadRequest("Invalid from format. Use YYYY-MM-DD.");
            }
            fromUtc = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            if (!DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            {
                return BadRequest("Invalid to format. Use YYYY-MM-DD.");
            }
            toUtc = DateTime.SpecifyKind(parsedTo.AddDays(1), DateTimeKind.Utc); // End of day
        }

        var observations = _observationRepository.GetByFlightIdWithDateFilter(flightId, fromUtc, toUtc);
        var dtos = observations.Select(o => new ObservationResponseDto
        {
            ObservedAtLocal = _statsService.ConvertUtcToZurich(o.ObservedAtUtc),
            PriceChf = o.PriceChf
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{flightId}/stats/weekday")]
    public ActionResult<WeekdayStatsResponseDto> GetWeekdayStats(string flightId)
    {
        var flight = _flightRepository.GetById(flightId);
        if (flight == null)
        {
            return NotFound("Flight not found");
        }

        var observations = _observationRepository.GetByFlightId(flightId);
        var stats = _statsService.ComputeWeekdayStats(flight, observations);
        return Ok(stats);
    }

    [HttpGet("{flightId}/stats/booking-date")]
    public ActionResult<BookingDateStatsResponseDto> GetBookingDateStats(
        string flightId,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var flight = _flightRepository.GetById(flightId);
        if (flight == null)
        {
            return NotFound("Flight not found");
        }

        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (!string.IsNullOrWhiteSpace(from))
        {
            if (!DateTime.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            {
                return BadRequest("Invalid from format. Use YYYY-MM-DD.");
            }
            fromUtc = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            if (!DateTime.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            {
                return BadRequest("Invalid to format. Use YYYY-MM-DD.");
            }
            toUtc = DateTime.SpecifyKind(parsedTo.AddDays(1), DateTimeKind.Utc);
        }

        var observations = _observationRepository.GetByFlightIdWithDateFilter(flightId, fromUtc, toUtc);
        var stats = _statsService.ComputeBookingDateStats(flight, observations);
        return Ok(stats);
    }

    [HttpGet("{flightId}/stats/days-to-departure")]
    public ActionResult<DaysToDepartureStatsResponseDto> GetDaysToDepartureStats(
        string flightId,
        [FromQuery] int bucket = 1)
    {
        if (bucket != 1 && bucket != 3 && bucket != 7)
        {
            return BadRequest("Bucket must be 1, 3, or 7");
        }

        var flight = _flightRepository.GetById(flightId);
        if (flight == null)
        {
            return NotFound("Flight not found");
        }

        var observations = _observationRepository.GetByFlightId(flightId);
        var stats = _statsService.ComputeDaysToDepartureStats(flight, observations, bucket);
        return Ok(stats);
    }
}
