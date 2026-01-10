namespace FlightTracker.Api.Infrastructure;

/// <summary>
/// Provides current time for testing purposes
/// </summary>
public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// System time provider using DateTimeOffset.UtcNow
/// </summary>
public class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
