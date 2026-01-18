using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FlightTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleRepository _scheduleRepository;

    public ScheduleController(IScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    [HttpGet]
    public ActionResult<List<ScheduleTimeDto>> GetSchedule()
    {
        var schedules = _scheduleRepository.GetAll();
        var dtos = schedules.Select(s => new ScheduleTimeDto
        {
            Hour = s.Time.Hour,
            Minute = s.Time.Minute
        }).ToList();

        return Ok(dtos);
    }

    [HttpPut]
    public IActionResult UpdateSchedule([FromBody] UpdateScheduleRequestDto request)
    {
        if (request.Times == null || request.Times.Count == 0)
        {
            return BadRequest("At least one schedule time is required");
        }

        // Validate times
        foreach (var time in request.Times)
        {
            if (time.Hour < 0 || time.Hour > 23)
            {
                return BadRequest($"Invalid hour: {time.Hour}. Must be between 0 and 23");
            }
            if (time.Minute < 0 || time.Minute > 59)
            {
                return BadRequest($"Invalid minute: {time.Minute}. Must be between 0 and 59");
            }
        }

        var times = request.Times.Select(t => new TimeOnly(t.Hour, t.Minute));
        _scheduleRepository.SetSchedules(times);

        return NoContent();
    }
}
