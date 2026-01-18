using System.Reflection;
using FlightTracker.Api.Services.Background;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using Moq;

namespace Backend.Tests.Services.Background;

public class DatabaseScheduleConfigProviderTests
{
    [Fact]
    public async Task GetScheduleAsync_WhenSchedulesExist_ReturnsConfigWithTimes()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var schedules = new[]
        {
            new ScrapeSchedule { Id = 1, Time = new TimeOnly(9, 0) },
            new ScrapeSchedule { Id = 2, Time = new TimeOnly(15, 30) },
            new ScrapeSchedule { Id = 3, Time = new TimeOnly(21, 0) }
        };
        mockRepository.Setup(r => r.GetAll()).Returns(schedules);
        var provider = new DatabaseScheduleConfigProvider(mockRepository.Object);

        // Act
        var result = await provider.GetScheduleAsync();

        // Assert
        Assert.Equal("Europe/Zurich", result.TimeZoneId);
        Assert.Equivalent(new[] { "09:00", "15:30", "21:00" }, result.TimesOfDay);
    }

    [Fact]
    public async Task GetScheduleAsync_WhenNoSchedules_ReturnsDefaultTimes()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        mockRepository.Setup(r => r.GetAll()).Returns(Array.Empty<ScrapeSchedule>());
        var provider = new DatabaseScheduleConfigProvider(mockRepository.Object);

        // Act
        var result = await provider.GetScheduleAsync();

        // Assert
        Assert.Equal("Europe/Zurich", result.TimeZoneId);
        Assert.Equivalent(new[] { "09:00", "21:00" }, result.TimesOfDay);
    }

    [Fact]
    public async Task GetScheduleAsync_CallsRepositoryGetAll()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        mockRepository.Setup(r => r.GetAll()).Returns(Array.Empty<ScrapeSchedule>());
        var provider = new DatabaseScheduleConfigProvider(mockRepository.Object);

        // Act
        await provider.GetScheduleAsync();

        // Assert
        mockRepository.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public async Task GetScheduleAsync_WithSingleSchedule_ReturnsSingleTime()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var schedules = new[]
        {
            new ScrapeSchedule { Id = 1, Time = new TimeOnly(12, 0) }
        };
        mockRepository.Setup(r => r.GetAll()).Returns(schedules);
        var provider = new DatabaseScheduleConfigProvider(mockRepository.Object);

        // Act
        var result = await provider.GetScheduleAsync();

        // Assert
        Assert.Single(result.TimesOfDay);
        Assert.Equal("12:00", result.TimesOfDay[0]);
    }

    [Fact]
    public async Task GetScheduleAsync_FormatsTimesWithLeadingZeros()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var schedules = new[]
        {
            new ScrapeSchedule { Id = 1, Time = new TimeOnly(5, 5) }
        };
        mockRepository.Setup(r => r.GetAll()).Returns(schedules);
        var provider = new DatabaseScheduleConfigProvider(mockRepository.Object);

        // Act
        var result = await provider.GetScheduleAsync();

        // Assert
        Assert.Equal("05:05", result.TimesOfDay[0]);
    }
}
