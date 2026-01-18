using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using LiteDB;
using Xunit;

namespace Backend.Tests.Repositories;

public class FlightRepositoryTests : IDisposable
{
    private readonly LiteDbContext _context;
    private readonly FlightRepository _repository;

    public FlightRepositoryTests()
    {
        // Use in-memory database for testing
        _context = new LiteDbContext(":memory:");
        _repository = new FlightRepository(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region GetById Tests

    [Fact]
    public void GetById_ValidId_ShouldReturnFlight()
    {
        // Arrange
        var flight = CreateAndInsertFlight("LX123", "ZRH", "JFK");

        // Act
        var result = _repository.GetById(flight.Id.ToString());

        // Assert
        Assert.Equivalent(flight, result);
    }

    [Fact]
    public void GetById_InvalidId_ShouldReturnNull()
    {
        // Act
        var result = _repository.GetById("invalid-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetById_NullId_ShouldReturnNull()
    {
        // Act
        var result = _repository.GetById(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetById_EmptyString_ShouldReturnNull()
    {
        // Act
        var result = _repository.GetById("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetById_NonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = ObjectId.NewObjectId().ToString();

        // Act
        var result = _repository.GetById(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Query Tests

    [Fact]
    public void Query_NoFilters_ShouldReturnAllFlights()
    {
        // Arrange
        CreateAndInsertFlight("LX123", "ZRH", "JFK");
        CreateAndInsertFlight("LX456", "ZRH", "LHR");
        CreateAndInsertFlight("LX789", "GVA", "JFK");

        // Act
        var result = _repository.Query();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Query_FilterByOrigin_ShouldReturnMatchingFlights()
    {
        // Arrange
        CreateAndInsertFlight("LX123", "ZRH", "JFK");
        CreateAndInsertFlight("LX456", "ZRH", "LHR");
        CreateAndInsertFlight("LX789", "GVA", "JFK");

        // Act
        var result = _repository.Query(origin: "ZRH");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.Equal("ZRH", f.OriginIata));
    }

    [Fact]
    public void Query_FilterByDestination_ShouldReturnMatchingFlights()
    {
        // Arrange
        CreateAndInsertFlight("LX123", "ZRH", "JFK");
        CreateAndInsertFlight("LX456", "ZRH", "LHR");
        CreateAndInsertFlight("LX789", "GVA", "JFK");

        // Act
        var result = _repository.Query(destination: "JFK");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.Equal("JFK", f.DestinationIata));
    }

    [Fact]
    public void Query_FilterByDepartureDate_ShouldReturnMatchingFlights()
    {
        // Arrange
        var date1 = new DateTime(2026, 1, 15);
        var date2 = new DateTime(2026, 1, 20);

        CreateAndInsertFlight("LX123", "ZRH", "JFK", date1);
        CreateAndInsertFlight("LX456", "ZRH", "LHR", date1);
        CreateAndInsertFlight("LX789", "GVA", "JFK", date2);

        // Act
        var result = _repository.Query(departureDate: date1);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.Equal(date1.Date, f.DepartureDate.Date));
    }

    [Fact]
    public void Query_FilterByFlightNumber_ShouldReturnMatchingFlights()
    {
        // Arrange
        CreateAndInsertFlight("LX123", "ZRH", "JFK");
        CreateAndInsertFlight("LX456", "ZRH", "LHR");
        CreateAndInsertFlight("LX123", "GVA", "JFK"); // Same flight number, different route

        // Act
        var result = _repository.Query(flightNumber: "LX123");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, f => Assert.Equal("LX123", f.FlightNumber));
    }

    [Fact]
    public void Query_CombinedFilters_ShouldReturnMatchingFlights()
    {
        // Arrange
        var date = new DateTime(2026, 1, 15);
        var correctFlight = CreateAndInsertFlight("LX123", "ZRH", "JFK", date);
        CreateAndInsertFlight("LX123", "ZRH", "LHR", date);
        CreateAndInsertFlight("LX123", "ZRH", "JFK", new DateTime(2026, 1, 20));
        var expected = new List<FlightEntity> { correctFlight };

        // Act
        var result = _repository.Query(
            origin: "ZRH",
            destination: "JFK",
            departureDate: date,
            flightNumber: "LX123");

        // Assert
        Assert.Equivalent(expected, result);
    }

    [Fact]
    public void Query_EmptyStringFilters_ShouldTreatAsNoFilter()
    {
        // Arrange
        CreateAndInsertFlight("LX123", "ZRH", "JFK");
        CreateAndInsertFlight("LX456", "GVA", "LHR");

        // Act
        var result = _repository.Query(origin: "", destination: "", flightNumber: "");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Query_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        CreateAndInsertFlight("LX123", "ZRH", "JFK");

        // Act
        var result = _repository.Query(origin: "LON");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region FindUnique Tests

    [Fact]
    public void FindUnique_ExactMatch_ShouldReturnFlight()
    {
        // Arrange
        var date = new DateTime(2026, 1, 15, 10, 30, 0);
        var expected = CreateAndInsertFlight("LX123", "ZRH", "JFK", date);

        // Act
        var result = _repository.FindUnique("LX123", date, "ZRH", "JFK");

        // Assert
        Assert.Equivalent(expected, result);
    }

    [Fact]
    public void FindUnique_SameDayDifferentTime_ShouldReturnFlight()
    {
        // Arrange
        var insertDate = new DateTime(2026, 1, 15, 10, 30, 0);
        var queryDate = new DateTime(2026, 1, 15, 14, 45, 0);
        CreateAndInsertFlight("LX123", "ZRH", "JFK", insertDate);

        // Act
        var result = _repository.FindUnique("LX123", queryDate, "ZRH", "JFK");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void FindUnique_DifferentDate_ShouldReturnNull()
    {
        // Arrange
        var insertDate = new DateTime(2026, 1, 15);
        var queryDate = new DateTime(2026, 1, 16);
        CreateAndInsertFlight("LX123", "ZRH", "JFK", insertDate);

        // Act
        var result = _repository.FindUnique("LX123", queryDate, "ZRH", "JFK");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindUnique_DifferentFlightNumber_ShouldReturnNull()
    {
        // Arrange
        var date = new DateTime(2026, 1, 15);
        CreateAndInsertFlight("LX123", "ZRH", "JFK", date);

        // Act
        var result = _repository.FindUnique("LX456", date, "ZRH", "JFK");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindUnique_DifferentRoute_ShouldReturnNull()
    {
        // Arrange
        var date = new DateTime(2026, 1, 15);
        CreateAndInsertFlight("LX123", "ZRH", "JFK", date);

        // Act
        var result = _repository.FindUnique("LX123", date, "ZRH", "LHR");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindUnique_MultipleFlightsSameNumber_ShouldReturnCorrectOne()
    {
        // Arrange
        var date1 = new DateTime(2026, 1, 15);
        var date2 = new DateTime(2026, 1, 16);
        CreateAndInsertFlight("LX123", "ZRH", "JFK", date1);
        var expected = CreateAndInsertFlight("LX123", "ZRH", "JFK", date2);

        // Act
        var result = _repository.FindUnique("LX123", date2, "ZRH", "JFK");

        // Assert
        Assert.Equivalent(expected, result);
    }

    #endregion

    #region Insert and Update Tests

    [Fact]
    public void Insert_ValidFlight_ShouldAddToDatabase()
    {
        // Arrange
        var flight = CreateAndInsertFlight("LX123", "ZRH", "JFK");

        // Act
        var result = _repository.GetById(flight.Id.ToString());

        // Assert
        Assert.Equivalent(flight, result);
    }

    [Fact]
    public void Insert_NullFlight_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _repository.Insert(null));
    }

    [Fact]
    public void Update_ExistingFlight_ShouldModifyData()
    {
        // Arrange
        var flight = CreateAndInsertFlight("LX123", "ZRH", "JFK");
        flight.FlightNumber = "LX999";

        // Act
        _repository.Update(flight);
        var retrieved = _repository.GetById(flight.Id.ToString());

        // Assert
        Assert.Equal("LX999", retrieved.FlightNumber);
    }

    [Fact]
    public void Update_NullFlight_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _repository.Update(null));
    }

    #endregion

    #region Helper Methods

    private FlightEntity CreateAndInsertFlight(
        string flightNumber,
        string origin,
        string destination,
        DateTime? departureDate = null)
    {
        var flight = new FlightEntity
        {
            FlightNumber = flightNumber,
            DepartureDate = departureDate ?? new DateTime(2026, 2, 1, 10, 30, 0),
            OriginIata = origin,
            DestinationIata = destination
        };

        _repository.Insert(flight);
        return flight;
    }

    #endregion
}
