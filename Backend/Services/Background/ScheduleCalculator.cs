using System.Globalization;

namespace FlightTracker.Api.Services.Background;

/// <summary>
/// Pure logic for computing next scheduled run times
/// </summary>
public static class ScheduleCalculator
{
    /// <summary>
    /// Computes the next scheduled run time in UTC based on configuration and current time
    /// </summary>
    public static DateTimeOffset ComputeNextRunUtc(ScheduleConfig config, DateTimeOffset nowUtc)
    {
        TimeZoneInfo timezone = ResolveTimeZone(config.TimeZoneId);

        DateTime nowLocal = TimeZoneInfo.ConvertTime(nowUtc.UtcDateTime, timezone);
        DateTime today = nowLocal.Date;

        // parse + sort times
        List<TimeSpan> times = config.TimesOfDay
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
        // this has issues around daylight saving time transitions but I don't care since this is a school project
        DateTimeOffset nextLocalOffset = new DateTimeOffset(nextLocal, timezone.GetUtcOffset(nextLocal));
        DateTimeOffset nextUtc = nextLocalOffset.ToUniversalTime();
        return nextUtc;
    }

    /// <summary>
    /// Parses time of day string in HH:mm format
    /// </summary>
    public static TimeSpan ParseTimeOfDay(string timeString)
    {
        if (string.IsNullOrWhiteSpace(timeString))
            throw new ArgumentException("Time string cannot be null or empty", nameof(timeString));

        // accepts "HH:mm" (e.g., "01:00", "13:30")
        if (!TimeSpan.TryParseExact(timeString, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan ts))
            throw new FormatException($"invalid time_of_day '{timeString}', expected HH:mm");

        return ts;
    }

    /// <summary>
    /// Resolves timezone supporting both IANA (Linux) and Windows timezone IDs
    /// </summary>
    public static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Timezone ID cannot be null or empty", nameof(timeZoneId));

        // DB stores IANA timezone IDs (e.g., "Europe/Zurich")
        // We need to convert to Windows IDs on Windows systems

        // Try ID as-is first (works on Linux with IANA IDs)
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Not found so probably on Windows, try converting IANA -> Windows
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
}
