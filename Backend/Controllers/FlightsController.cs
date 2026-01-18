using FlightTracker.Contracts;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Services;
using System.Globalization;

namespace FlightTracker.Api.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController(
    IFlightRepository flightRepository,
    IObservationRepository observationRepository,
    IFlightStatsService statsService,
    IQueryRepository queryRepository) : ControllerBase
{
    private readonly IFlightRepository _flightRepository = flightRepository;
    private readonly IObservationRepository _observationRepository = observationRepository;
    private readonly IFlightStatsService _statsService = statsService;
    private readonly IQueryRepository _queryRepository = queryRepository;

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
        var flight = _flightRepository.GetById(flightId);
        if (flight == null)
        {
            return NotFound("Flight not found");
        }

        var observations = _observationRepository.GetByFlightId(flightId);
        var stats = _statsService.ComputeDaysToDepartureStats(flight, observations, bucket);
        return Ok(stats);
    }

    [HttpPost("query")]
    public IActionResult AddNewFlightQuery(FlightQueryDto dto)
    {
        if(dto.DepartureDate == null)
        {
            return BadRequest("DepartureDate is required.");
        }
        var entity = new QueryEntity()
        {
            DestinationIata = dto.DestinationIata,
            OriginIata = dto.OriginIata,
            AnchorDate = dto.DepartureDate.Value,
            FlexibilityDays = dto.FlexibilityDays
        };
        _queryRepository.Insert(entity);
        return Accepted();
    }

    [HttpGet("queries")]
    public ActionResult<List<QueryResponseDto>> GetQueries()
    {
        var entities = _queryRepository.GetAll();
        var dtos = entities.Select(q => new QueryResponseDto { Id = q.Id.ToString(), OriginIata = q.OriginIata, DestinationIata = q.DestinationIata, DepartureDate = q.AnchorDate, FlexibilityDays = q.FlexibilityDays }).ToList();
        return Ok(dtos);
    }

    [HttpDelete("queries/{id}")]
    public IActionResult DeleteQuery(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest("id is required");

        var ok = _queryRepository.Delete(id);
        if (ok)
            return NoContent();

        return NotFound();
    }

}
