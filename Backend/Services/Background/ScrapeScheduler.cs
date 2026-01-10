using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using FlightTracker.Api.Infrastructure;

namespace FlightTracker.Api.Services.Background;

public sealed class ScrapeScheduler : BackgroundService
{
    private readonly Channel<bool> _reschedule = Channel.CreateUnbounded<bool>();
    private readonly IScheduleConfigProvider _configProvider;
    private readonly IFlightScrapingService _scrapingService;
    private readonly ITimeProvider _timeProvider;

    public ScrapeScheduler(
        IScheduleConfigProvider configProvider,
        IFlightScrapingService scrapingService,
        ITimeProvider timeProvider)
    {
        _configProvider = configProvider;
        _scrapingService = scrapingService;
        _timeProvider = timeProvider;
    }

    public void Reschedule() => _reschedule.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ScheduleConfig cfg = await _configProvider.GetScheduleAsync(stoppingToken).ConfigureAwait(false);

            DateTimeOffset nextRunUtc = ScheduleCalculator.ComputeNextRunUtc(cfg, _timeProvider.UtcNow);
            TimeSpan delay = nextRunUtc - _timeProvider.UtcNow;
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

            Task delayTask = Task.Delay(delay, stoppingToken);
            Task signalTask = _reschedule.Reader.ReadAsync(stoppingToken).AsTask();

            Task completed = await Task.WhenAny(delayTask, signalTask);

            // check for shutdown before doing any work
            if (stoppingToken.IsCancellationRequested)
                break;

            if (completed == signalTask)
            {
                DrainSignals();
                continue; // schedule changed -> re-read db and recompute
            }

            await _scrapingService.ScrapeFlightsAsync(stoppingToken).ConfigureAwait(false);

            // after job, loop again and re-read schedule from db
        }
    }

    private void DrainSignals()
    {
        while (_reschedule.Reader.TryRead(out _)) { }
    }
}

public sealed class ScheduleConfig
{
    public required string TimeZoneId { get; init; }          // always "Europe/Zurich" for now
    public required List<string> TimesOfDay { get; init; }    // e.g. ["01:00", "13:00", "19:00"]
}
