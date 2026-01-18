using FlightTracker.Api.Controllers;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Contracts;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Backend.Tests.Controllers;

public class ScheduleControllerTests
{
    [Fact]
    public void GetSchedule_ReturnsSchedulesAsDtos()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var schedules = new[]
        {
            new ScrapeSchedule { Id = 1, Time = new TimeOnly(9, 0) },
            new ScrapeSchedule { Id = 2, Time = new TimeOnly(21, 0) }
        };
        mockRepository.Setup(r => r.GetAll()).Returns(schedules);
        var controller = new ScheduleController(mockRepository.Object);

        // Act
        var result = controller.GetSchedule();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<ScheduleTimeDto>>(okResult.Value);
        Assert.Equivalent(dtos, schedules.Select(s => new ScheduleTimeDto
        {
            Hour = s.Time.Hour,
            Minute = s.Time.Minute
        }));
    }

    [Fact]
    public void GetSchedule_WhenNoSchedules_ReturnsEmptyList()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        mockRepository.Setup(r => r.GetAll()).Returns(Array.Empty<ScrapeSchedule>());
        var controller = new ScheduleController(mockRepository.Object);

        // Act
        var result = controller.GetSchedule();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsType<List<ScheduleTimeDto>>(okResult.Value);
        Assert.Empty(dtos);
    }

    [Fact]
    public void UpdateSchedule_WithValidTimes_ReturnsNoContent()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = 8, Minute = 30 },
                new() { Hour = 16, Minute = 45 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
        mockRepository.Verify(r => r.SetSchedules(It.Is<IEnumerable<TimeOnly>>(times =>
            times.Contains(new TimeOnly(8, 30)) &&
            times.Contains(new TimeOnly(16, 45)) &&
            times.Count() == 2
        )), Times.Once);
    }

    [Fact]
    public void UpdateSchedule_WithEmptyList_ReturnsBadRequest()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto { Times = new List<ScheduleTimeDto>() };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("At least one schedule time is required", badRequest.Value);
        mockRepository.Verify(r => r.SetSchedules(It.IsAny<IEnumerable<TimeOnly>>()), Times.Never);
    }

    [Fact]
    public void UpdateSchedule_WithNullTimes_ReturnsBadRequest()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto { Times = null! };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("At least one schedule time is required", badRequest.Value);
    }

    [Fact]
    public void UpdateSchedule_WithInvalidHour_ReturnsBadRequest()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = 25, Minute = 0 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid hour", badRequest.Value?.ToString());
    }

    [Fact]
    public void UpdateSchedule_WithNegativeHour_ReturnsBadRequest()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = -1, Minute = 0 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid hour", badRequest.Value?.ToString());
    }

    [Fact]
    public void UpdateSchedule_WithInvalidMinute_ReturnsBadRequest()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = 12, Minute = 60 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid minute", badRequest.Value?.ToString());
    }

    [Fact]
    public void UpdateSchedule_WithNegativeMinute_ReturnsBadRequest()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = 12, Minute = -5 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid minute", badRequest.Value?.ToString());
    }

    [Fact]
    public void UpdateSchedule_WithMidnight_Succeeds()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = 0, Minute = 0 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void UpdateSchedule_WithEndOfDay_Succeeds()
    {
        // Arrange
        var mockRepository = new Mock<IScheduleRepository>();
        var controller = new ScheduleController(mockRepository.Object);
        var request = new UpdateScheduleRequestDto
        {
            Times = new List<ScheduleTimeDto>
            {
                new() { Hour = 23, Minute = 59 }
            }
        };

        // Act
        var result = controller.UpdateSchedule(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
