using FlightTracker.Api.Storage.Repositories;

namespace FlightTracker.Api.Services.Background;

public class DatabaseScheduleConfigProvider : IScheduleConfigProvider
{
    private readonly IScheduleRepository _scheduleRepository;

    public DatabaseScheduleConfigProvider(IScheduleRepository scheduleRepository)
    {
        _scheduleRepository = scheduleRepository;
    }

    public Task<ScheduleConfig> GetScheduleAsync(CancellationToken cancellationToken = default)
    {
        var schedules = _scheduleRepository.GetAll();
        var times = schedules.Select(s => s.Time).ToList();

        // Default to 9 AM and 9 PM if no schedules configured
        if (times.Count == 0)
        {
            times = new List<TimeOnly> { new(9, 0), new(21, 0) };
        }

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = times.Select(t => $"{t.Hour:D2}:{t.Minute:D2}").ToList()
        };

        return Task.FromResult(config);
    }
}
