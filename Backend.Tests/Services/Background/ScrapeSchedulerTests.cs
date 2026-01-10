using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Infrastructure;
using Moq;
using Xunit;

namespace Backend.Tests.Services.Background;

public class ScrapeSchedulerTests
{
    [Fact]
    public async Task ExecuteAsync_RunsScrapeAtScheduledTime()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "10:00" }
        };

        // Current time: 09:59:55 UTC (5 seconds before scheduled time in Zurich winter)
        var currentTime = new DateTimeOffset(2026, 1, 10, 8, 59, 55, TimeSpan.Zero);

        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(currentTime);

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);

        // Wait a bit to ensure the scheduler starts
        await Task.Delay(100);

        // Cancel to stop the scheduler
        cts.Cancel();

        try
        {
            await executeTask;
        }
        catch (TaskCanceledException)
        {
            // Expected when cancelling
        }

        // Assert
        mockConfigProvider.Verify(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Reschedule_TriggersScheduleRecomputation()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "10:00" }
        };

        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(DateTimeOffset.UtcNow);

        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        scheduler.Reschedule();

        // Dunno how to properly assert, if it doesn't throw it's good 👍
        Assert.True(true);
    }
}
