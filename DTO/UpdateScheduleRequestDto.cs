namespace FlightTracker.Contracts;

public class UpdateScheduleRequestDto
{
    public List<ScheduleTimeDto> Times { get; set; } = new();
}
