namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Provides schedule configuration
/// </summary>
public interface IScheduleConfigProvider
{
    Task<ScheduleConfig> GetScheduleAsync(CancellationToken cancellationToken = default);
}
