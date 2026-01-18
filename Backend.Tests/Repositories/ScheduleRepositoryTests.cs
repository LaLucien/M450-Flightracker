using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Repositories;

namespace Backend.Tests.Repositories;

public class ScheduleRepositoryTests
{
    private LiteDbContext CreateInMemoryContext()
    {
        return new LiteDbContext(":memory:");
    }

    [Fact]
    public void GetAll_WhenNoSchedules_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScheduleRepository(context);

        // Act
        var result = repository.GetAll();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void SetSchedules_StoresSchedulesInDatabase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScheduleRepository(context);
        var times = new[]
        {
            new TimeOnly(9, 0),
            new TimeOnly(14, 30),
            new TimeOnly(21, 0)
        };

        // Act
        repository.SetSchedules(times);
        var result = repository.GetAll().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equivalent(times, result.Select(s => s.Time));
    }

    [Fact]
    public void SetSchedules_ReplacesExistingSchedules()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScheduleRepository(context);
        var initialTimes = new[] { new TimeOnly(9, 0), new TimeOnly(21, 0) };
        var newTimes = new[] { new TimeOnly(10, 0) };

        // Act
        repository.SetSchedules(initialTimes);
        repository.SetSchedules(newTimes);
        var result = repository.GetAll().ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(new TimeOnly(10, 0), result[0].Time);
    }

    [Fact]
    public void SetSchedules_WithEmptyList_ClearsAllSchedules()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScheduleRepository(context);
        var times = new[] { new TimeOnly(9, 0) };
        repository.SetSchedules(times);

        // Act
        repository.SetSchedules(Array.Empty<TimeOnly>());
        var result = repository.GetAll();

        // Assert
        Assert.Empty(result);
    }
}
