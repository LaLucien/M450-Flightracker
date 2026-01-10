namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Default implementation that returns a hardcoded schedule
/// TODO: Replace with database-backed implementation
/// </summary>
public class DefaultScheduleConfigProvider : IScheduleConfigProvider
{
    public Task<ScheduleConfig> GetScheduleAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("implement reading schedule from database");
    }
}
