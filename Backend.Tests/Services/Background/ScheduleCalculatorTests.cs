using FlightTracker.Api.Services.Background;
using Xunit;

namespace Backend.Tests.Services.Background;

public class ScheduleCalculatorTests
{
    [Fact]
    public void ParseTimeOfDay_ValidTime_ReturnsCorrectTimeSpan()
    {
        // Arrange
        string timeString = "13:30";

        // Act
        TimeSpan result = ScheduleCalculator.ParseTimeOfDay(timeString);

        // Assert
        Assert.Equal(new TimeSpan(13, 30, 0), result);
    }

    [Theory]
    [InlineData("01:00")]
    [InlineData("00:00")]
    [InlineData("23:59")]
    public void ParseTimeOfDay_ValidTimes_ParsesSuccessfully(string timeString)
    {
        // Act & Assert - should not throw
        var result = ScheduleCalculator.ParseTimeOfDay(timeString);
        Assert.True(result >= TimeSpan.Zero);
        Assert.True(result < TimeSpan.FromDays(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ParseTimeOfDay_NullOrEmpty_ThrowsArgumentException(string? timeString)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ScheduleCalculator.ParseTimeOfDay(timeString!));
    }

    [Theory]
    [InlineData("25:00")]
    [InlineData("13:60")]
    [InlineData("invalid")]
    [InlineData("1:30")]  // Missing leading zero
    public void ParseTimeOfDay_InvalidFormat_ThrowsFormatException(string timeString)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => ScheduleCalculator.ParseTimeOfDay(timeString));
    }

    [Fact]
    public void ResolveTimeZone_EuropeZurich_ReturnsValidTimeZone()
    {
        // Arrange
        string timeZoneId = "Europe/Zurich";

        // Act
        TimeZoneInfo result = ScheduleCalculator.ResolveTimeZone(timeZoneId);

        // Assert
        Assert.NotNull(result);
        // Verify it's the correct timezone (UTC+1 standard, UTC+2 DST)
        Assert.True(result.BaseUtcOffset == TimeSpan.FromHours(1) ||
                   result.BaseUtcOffset == TimeSpan.FromHours(2));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void ResolveTimeZone_NullOrEmpty_ThrowsArgumentException(string? timeZoneId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ScheduleCalculator.ResolveTimeZone(timeZoneId!));
    }

    [Fact]
    public void ResolveTimeZone_UnsupportedTimezone_ThrowsNotImplementedException()
    {
        string unsupportedTimezone = "Invalid/Timezone_XYZ123";

        // Act & Assert
        var exception = Assert.Throws<NotImplementedException>(
            () => ScheduleCalculator.ResolveTimeZone(unsupportedTimezone));

        Assert.Contains("not supported", exception.Message);
    }

    [Fact]
    public void ComputeNextRunUtc_BeforeFirstScheduledTime_ReturnsFirstTimeToday()
    {
        // Arrange
        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "10:00", "14:00", "18:00" }
        };

        // Simulate current time: 2026-01-10 08:00 Zurich time (07:00 UTC in winter)
        var nowUtc = new DateTimeOffset(2026, 1, 10, 7, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset nextRun = ScheduleCalculator.ComputeNextRunUtc(config, nowUtc);

        // Assert
        // Should schedule for 10:00 Zurich time = 09:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRunUtc_BetweenScheduledTimes_ReturnsNextTime()
    {
        // Arrange
        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "10:00", "14:00", "18:00" }
        };

        // Simulate current time: 2026-01-10 12:00 Zurich time (11:00 UTC)
        var nowUtc = new DateTimeOffset(2026, 1, 10, 11, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset nextRun = ScheduleCalculator.ComputeNextRunUtc(config, nowUtc);

        // Assert
        // Should schedule for 14:00 Zurich time = 13:00 UTC
        Assert.Equal(new DateTimeOffset(2026, 1, 10, 13, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRunUtc_AfterAllScheduledTimes_ReturnsFirstTimeTomorrow()
    {
        // Arrange
        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "10:00", "14:00", "18:00" }
        };

        // Simulate current time: 2026-01-10 20:00 Zurich time (19:00 UTC)
        var nowUtc = new DateTimeOffset(2026, 1, 10, 19, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset nextRun = ScheduleCalculator.ComputeNextRunUtc(config, nowUtc);

        // Assert
        // Should schedule for 10:00 Zurich time tomorrow = 09:00 UTC next day
        Assert.Equal(new DateTimeOffset(2026, 1, 11, 9, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRunUtc_UnsortedTimes_HandlesCorrectly()
    {
        // Arrange
        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "18:00", "10:00", "14:00" }  // Unsorted!
        };

        // Simulate current time: 2026-01-10 08:00 Zurich time (07:00 UTC)
        var nowUtc = new DateTimeOffset(2026, 1, 10, 7, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset nextRun = ScheduleCalculator.ComputeNextRunUtc(config, nowUtc);

        // Assert
        // Should still pick 10:00 as the next time
        Assert.Equal(new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero), nextRun);
    }

    [Fact]
    public void ComputeNextRunUtc_EmptyTimesList_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string>()  // Empty!
        };
        var nowUtc = DateTimeOffset.UtcNow;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => ScheduleCalculator.ComputeNextRunUtc(config, nowUtc));
    }

    [Fact]
    public void ComputeNextRunUtc_SingleTime_WorksCorrectly()
    {
        // Arrange
        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "03:00" }
        };

        // Before the scheduled time
        var nowUtc = new DateTimeOffset(2026, 1, 10, 1, 0, 0, TimeSpan.Zero);

        // Act
        DateTimeOffset nextRun = ScheduleCalculator.ComputeNextRunUtc(config, nowUtc);

        // Assert
        // Should schedule for 03:00 Zurich time = 02:00 UTC (winter time)
        Assert.Equal(new DateTimeOffset(2026, 1, 10, 2, 0, 0, TimeSpan.Zero), nextRun);
    }
}
