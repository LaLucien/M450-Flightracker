using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Infrastructure;
using Moq;
using Xunit;

namespace Backend.Tests.Services.Background;

public class ScrapeSchedulerTests
{
    [Fact]
    public async Task StartAsync_ShouldLoadConfigurationOnStart()
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
            .Returns(new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero));

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert
        mockConfigProvider.Verify(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_ShouldCallScrapingServiceWhenDelayCompletes()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "09:00" }
        };

        // Set time very close to scheduled time to minimize test delay
        var currentTime = new DateTimeOffset(2026, 1, 10, 7, 59, 59, 950, TimeSpan.Zero);

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

        // Wait for scrape to complete
        await Task.Delay(200);
        cts.Cancel();

        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert
        mockScrapingService.Verify(x => x.ScrapeFlightsAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Reschedule_ShouldInterruptDelayAndReloadConfig()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var initialConfig = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "23:00" }
        };

        var setupCallCount = 0;
        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                setupCallCount++;
                return initialConfig;
            });

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero));

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);

        scheduler.Reschedule();
        await Task.Delay(50);

        cts.Cancel();
        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert
        Assert.True(setupCallCount >= 2);
    }

    [Fact]
    public async Task Reschedule_ShouldNotCauseScrapeToExecuteImmediately()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "23:00" }
        };

        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero));

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);

        scheduler.Reschedule();
        await Task.Delay(100);

        cts.Cancel();
        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert
        mockScrapingService.Verify(x => x.ScrapeFlightsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldReloadConfigAfterEachScrape()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "09:00" }
        };

        var configCallCount = 0;
        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                configCallCount++;
                return config;
            });

        // Start just before scheduled time
        var currentTime = new DateTimeOffset(2026, 1, 10, 7, 59, 59, 950, TimeSpan.Zero);

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
        await Task.Delay(200);
        cts.Cancel();

        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert - config should be loaded before and after scrape
        Assert.True(configCallCount >= 2);
    }

    [Fact]
    public async Task StartAsync_ShouldHandleCancellationGracefully()
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
            .Returns(new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero));

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        var exception = await Record.ExceptionAsync(async () => await executeTask);

        // Assert
        Assert.True(exception is TaskCanceledException or OperationCanceledException or null);
    }

    [Fact]
    public async Task StartAsync_ShouldNotScrapeWhenCancelledDuringDelay()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "23:00" }
        };

        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero));

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert
        mockScrapingService.Verify(x => x.ScrapeFlightsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_ShouldUseTimeProviderForScheduleCalculation()
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

        var testTime = new DateTimeOffset(2026, 1, 10, 8, 30, 0, TimeSpan.Zero);

        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(testTime);

        var cts = new CancellationTokenSource();
        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        // Act
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        try { await executeTask; } catch (TaskCanceledException) { }

        // Assert
        mockTimeProvider.VerifyGet(x => x.UtcNow, Times.AtLeastOnce);
    }

    [Fact]
    public async Task Reschedule_CanBeCalledMultipleTimes()
    {
        // Arrange
        var mockConfigProvider = new Mock<IScheduleConfigProvider>();
        var mockScrapingService = new Mock<IFlightScrapingService>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        var config = new ScheduleConfig
        {
            TimeZoneId = "Europe/Zurich",
            TimesOfDay = new List<string> { "23:00" }
        };

        mockConfigProvider
            .Setup(x => x.GetScheduleAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        mockTimeProvider
            .SetupGet(x => x.UtcNow)
            .Returns(new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero));

        var scheduler = new ScrapeScheduler(
            mockConfigProvider.Object,
            mockScrapingService.Object,
            mockTimeProvider.Object);

        var cts = new CancellationTokenSource();
        var executeTask = scheduler.StartAsync(cts.Token);
        await Task.Delay(50);

        // Act - call multiple times
        scheduler.Reschedule();
        scheduler.Reschedule();
        scheduler.Reschedule();
        await Task.Delay(100);

        cts.Cancel();
        var exception = await Record.ExceptionAsync(async () => await executeTask);

        // Assert
        Assert.True(exception is TaskCanceledException or OperationCanceledException or null);
    }
}
