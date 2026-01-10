using System.Globalization;
using System.Threading.Channels;

namespace FlightTracker.Api.Services.Background;

public class FlightScrapingService
{
    // Placeholder for the actual flight collection service, I don't know which of the classes or interfaces I should actually use here
}

public sealed class ScrapeScheduler : BackgroundService
{
    private readonly Channel<bool> _reschedule = Channel.CreateUnbounded<bool>();
    private readonly FlightScrapingService _scrapingService;

    public ScrapeScheduler(FlightScrapingService scrapingService)
    {
        _scrapingService = scrapingService ?? throw new ArgumentNullException(nameof(scrapingService));
    }

    public void Reschedule() => _reschedule.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ScheduleConfig cfg = await LoadScheduleFromDbAsync(stoppingToken).ConfigureAwait(false);

            DateTimeOffset nextRunUtc = ComputeNextRunUtc(cfg, DateTimeOffset.UtcNow);
            TimeSpan delay = nextRunUtc - DateTimeOffset.UtcNow;
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

            await RunScrapeOnceAsync(stoppingToken).ConfigureAwait(false);

            // after job, loop again and re-read schedule from db
        }
    }

    private void DrainSignals()
    {
        while (_reschedule.Reader.TryRead(out _)) { }
    }

    private static DateTimeOffset ComputeNextRunUtc(ScheduleConfig cfg, DateTimeOffset nowUtc)
    {
        TimeZoneInfo timezone = ResolveTimeZone(cfg.TimeZoneId);

        DateTime nowLocal = TimeZoneInfo.ConvertTime(nowUtc.UtcDateTime, timezone);
        DateTime today = nowLocal.Date;

        // parse + sort times
        List<TimeSpan> times = cfg.TimesOfDay
            .Select(ParseTimeOfDay)
            .OrderBy(t => t)
            .ToList();

        if (times.Count == 0)
            throw new InvalidOperationException("schedule has no times_of_day");

        // pick next time today, else first time tomorrow
        DateTime nextLocal = default;
        bool found = false;

        foreach (TimeSpan t in times)
        {
            DateTime candidate = today.Add(t);
            if (candidate > nowLocal)
            {
                nextLocal = candidate;
                found = true;
                break;
            }
        }

        if (!found)
            nextLocal = today.AddDays(1).Add(times[0]);

        // convert local -> utc
        // note: this fails with daylight saving times. I don't care, this is a school project.
        DateTimeOffset nextLocalOffset = new DateTimeOffset(nextLocal, timezone.GetUtcOffset(nextLocal));
        DateTimeOffset nextUtc = nextLocalOffset.ToUniversalTime();
        return nextUtc;
    }

    private static TimeSpan ParseTimeOfDay(string s)
    {
        // accepts "HH:mm" (e.g., "01:00", "13:30")
        if (!TimeSpan.TryParseExact(s, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan ts))
            throw new FormatException($"invalid time_of_day '{s}', expected HH:mm");
        return ts;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        // DB stores IANA timezone IDs (e.g., "Europe/Zurich")
        // We need to convert to Windows IDs on Windows systems

        // Try the ID as-is first (works on Linux with IANA IDs)
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Not found - likely on Windows, try converting IANA -> Windows
            string? windowsId = ConvertIanaToWindows(timeZoneId);

            if (windowsId != null)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }

            throw new NotImplementedException(
                $"Timezone '{timeZoneId}' is not supported. " +
                "Only 'Europe/Zurich' is currently implemented for cross-platform support.");
        }
    }

    private static string? ConvertIanaToWindows(string ianaId)
    {
        // Hardcoded mapping for Europe/Zurich only
        // In prod we'd use a library for this but I don't wanna do that now
        return ianaId switch
        {
            "Europe/Zurich" => "W. Europe Standard Time",
            _ => null
        };
    }

    private static Task<ScheduleConfig> LoadScheduleFromDbAsync(CancellationToken ct)
    {
        throw new NotImplementedException("Load schedule from database");
    }

    private async Task RunScrapeOnceAsync(CancellationToken ct)
    {
        throw new NotImplementedException("call your scraping function here");
    }
}

public sealed class ScheduleConfig
{
    public required string TimeZoneId { get; init; }          // always "Europe/Zurich" for now
    public required List<string> TimesOfDay { get; init; }    // e.g. ["01:00", "13:00", "19:00"]
}
