using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using LiteDB;
using Xunit;

namespace Backend.Tests.Repositories;

public class ObservationRepositoryTests : IDisposable
{
    private readonly LiteDbContext _context;
    private readonly ObservationRepository _repository;
    private readonly FlightRepository _flightRepository;

    public ObservationRepositoryTests()
    {
        // Use in-memory database for testing
        _context = new LiteDbContext(":memory:");
        _repository = new ObservationRepository(_context);
        _flightRepository = new FlightRepository(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region GetByFlightId Tests

    [Fact]
    public void GetByFlightId_ValidId_ShouldReturnObservations()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), 110m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 12, 12, 0, 0, DateTimeKind.Utc), 120m);

        // Act
        var result = _repository.GetByFlightId(flight.Id.ToString());

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, obs => Assert.Equal(flight.Id, obs.FlightId));
    }

    [Fact]
    public void GetByFlightId_ShouldOrderByObservedAtUtc()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 300m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 12, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightId(flight.Id.ToString());

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].ObservedAtUtc <= result[1].ObservedAtUtc);
        Assert.True(result[1].ObservedAtUtc <= result[2].ObservedAtUtc);
    }

    [Fact]
    public void GetByFlightId_InvalidId_ShouldReturnEmptyList()
    {
        // Act
        var result = _repository.GetByFlightId("invalid-id");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetByFlightId_NonExistentId_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentId = ObjectId.NewObjectId().ToString();

        // Act
        var result = _repository.GetByFlightId(nonExistentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetByFlightId_MultipleFlights_ShouldReturnOnlyMatchingObservations()
    {
        // Arrange
        var flight1 = CreateAndInsertFlight();
        var flight2 = CreateAndInsertFlight();

        CreateAndInsertObservation(flight1.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight1.Id, new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), 110m);
        CreateAndInsertObservation(flight2.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightId(flight1.Id.ToString());

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, obs => Assert.Equal(flight1.Id, obs.FlightId));
    }

    #endregion

    #region GetByFlightIdWithDateFilter Tests

    [Fact]
    public void GetByFlightIdWithDateFilter_NoDateFilters_ShouldReturnAllObservations()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 150m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(flight.Id.ToString());

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_WithFromDate_ShouldFilterCorrectly()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var fromDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 150m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(flight.Id.ToString(), fromUtc: fromDate);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, obs => Assert.True(obs.ObservedAtUtc >= fromDate));
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_WithToDate_ShouldFilterCorrectly()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var toDate = new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 150m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(flight.Id.ToString(), toUtc: toDate);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, obs => Assert.True(obs.ObservedAtUtc < toDate));
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_WithBothDates_ShouldFilterCorrectly()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var fromDate = new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 1, 18, 0, 0, 0, DateTimeKind.Utc);

        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 150m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(
            flight.Id.ToString(),
            fromUtc: fromDate,
            toUtc: toDate);

        // Assert
        Assert.Single(result);
        Assert.Equal(150m, result[0].PriceChf);
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_BoundaryConditions_ShouldIncludeFromExcludeTo()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var fromDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

        CreateAndInsertObservation(flight.Id, fromDate, 150m); // Should be included
        CreateAndInsertObservation(flight.Id, toDate, 200m); // Should be excluded

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(
            flight.Id.ToString(),
            fromUtc: fromDate,
            toUtc: toDate);

        // Assert
        Assert.Single(result);
        Assert.Equal(150m, result[0].PriceChf);
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var fromDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(
            flight.Id.ToString(),
            fromUtc: fromDate,
            toUtc: toDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_InvalidFlightId_ShouldReturnEmptyList()
    {
        // Arrange
        var fromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(
            "invalid-id",
            fromUtc: fromDate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetByFlightIdWithDateFilter_ShouldOrderByObservedAtUtc()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 300m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m);
        CreateAndInsertObservation(flight.Id, new DateTime(2026, 1, 12, 12, 0, 0, DateTimeKind.Utc), 200m);

        // Act
        var result = _repository.GetByFlightIdWithDateFilter(flight.Id.ToString());

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].ObservedAtUtc <= result[1].ObservedAtUtc);
        Assert.True(result[1].ObservedAtUtc <= result[2].ObservedAtUtc);
    }

    #endregion

    #region Insert Tests

    [Fact]
    public void Insert_ValidObservation_ShouldAddToDatabase()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var observation = new ObservationEntity
        {
            FlightId = flight.Id,
            ObservedAtUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            PriceChf = 250m
        };

        // Act
        _repository.Insert(observation);
        var retrieved = _repository.GetByFlightId(flight.Id.ToString());

        // Assert
        Assert.Single(retrieved);
        Assert.Equal(250m, retrieved[0].PriceChf);
    }

    [Fact]
    public void Insert_NullObservation_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _repository.Insert(null));
    }

    [Fact]
    public void InsertMany_ValidObservations_ShouldAddAllToDatabase()
    {
        // Arrange
        var flight = CreateAndInsertFlight();
        var observations = new List<ObservationEntity>
        {
            new ObservationEntity
            {
                FlightId = flight.Id,
                ObservedAtUtc = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc),
                PriceChf = 100m
            },
            new ObservationEntity
            {
                FlightId = flight.Id,
                ObservedAtUtc = new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc),
                PriceChf = 110m
            },
            new ObservationEntity
            {
                FlightId = flight.Id,
                ObservedAtUtc = new DateTime(2026, 1, 12, 12, 0, 0, DateTimeKind.Utc),
                PriceChf = 120m
            }
        };

        // Act
        _repository.InsertMany(observations);
        var retrieved = _repository.GetByFlightId(flight.Id.ToString());

        // Assert
        Assert.Equal(3, retrieved.Count);
    }

    [Fact]
    public void InsertMany_NullCollection_ShouldNotThrowException()
    {
        // Act & Assert - Should handle gracefully
        _repository.InsertMany(null);
    }

    [Fact]
    public void InsertMany_EmptyCollection_ShouldNotThrowException()
    {
        // Act & Assert - Should handle gracefully
        _repository.InsertMany(new List<ObservationEntity>());
    }

    #endregion

    #region Helper Methods

    private FlightEntity CreateAndInsertFlight()
    {
        var flight = new FlightEntity
        {
            FlightNumber = "LX" + new Random().Next(100, 999),
            DepartureDate = new DateTime(2026, 2, 1, 10, 30, 0),
            OriginIata = "ZRH",
            DestinationIata = "JFK"
        };

        _flightRepository.Insert(flight);
        return flight;
    }

    private ObservationEntity CreateAndInsertObservation(ObjectId flightId, DateTime observedAtUtc, decimal price)
    {
        var observation = new ObservationEntity
        {
            FlightId = flightId,
            ObservedAtUtc = observedAtUtc,
            PriceChf = price
        };

        _repository.Insert(observation);
        return observation;
    }

    #endregion
}
