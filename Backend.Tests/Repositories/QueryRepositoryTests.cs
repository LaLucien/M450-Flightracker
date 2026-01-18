using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using LiteDB;
using Xunit;

namespace Backend.Tests.Repositories;

public class QueryRepositoryTests : IDisposable
{
    private readonly LiteDbContext _context;
    private readonly QueryRepository _repository;

    public QueryRepositoryTests()
    {
        _context = new LiteDbContext(":memory:");
        _repository = new QueryRepository(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new QueryRepository(null!));
        Assert.Equal("context", exception.ParamName);
    }

    #endregion

    #region Insert Tests

    [Fact]
    public void Insert_ValidQuery_ShouldInsertSuccessfully()
    {
        // Arrange
        var query = CreateQuery("ZRH", "JFK", 3);

        // Act
        _repository.Insert(query);

        // Assert
        var allQueries = _repository.GetAll();
        Assert.Single(allQueries);
    }

    [Fact]
    public void Insert_ValidQuery_ShouldGenerateId()
    {
        // Arrange
        var query = CreateQuery("ZRH", "JFK", 3);

        // Act
        _repository.Insert(query);

        // Assert
        Assert.NotEqual(ObjectId.Empty, query.Id);
    }

    [Fact]
    public void Insert_MultipleQueries_ShouldInsertAll()
    {
        // Arrange
        var query1 = CreateQuery("ZRH", "JFK", 3);
        var query2 = CreateQuery("GVA", "LHR", 5);
        var query3 = CreateQuery("ZRH", "BCN", 0);

        // Act
        _repository.Insert(query1);
        _repository.Insert(query2);
        _repository.Insert(query3);

        // Assert
        var allQueries = _repository.GetAll();
        Assert.Equal(3, allQueries.Count);
    }

    [Fact]
    public void Insert_SameOriginDestination_ShouldInsertBoth()
    {
        // Arrange
        var query1 = CreateQuery("ZRH", "JFK", 3, new DateTime(2026, 1, 15));
        var query2 = CreateQuery("ZRH", "JFK", 3, new DateTime(2026, 2, 15));

        // Act
        _repository.Insert(query1);
        _repository.Insert(query2);

        // Assert
        var allQueries = _repository.GetAll();
        Assert.Equal(2, allQueries.Count);
    }

    [Fact]
    public void Insert_ZeroFlexibilityDays_ShouldInsert()
    {
        // Arrange
        var query = CreateQuery("ZRH", "JFK", 0);

        // Act
        _repository.Insert(query);

        // Assert
        var allQueries = _repository.GetAll();
        var insertedQuery = Assert.Single(allQueries);
        Assert.Equal(0, insertedQuery.FlexibilityDays);
    }

    [Fact]
    public void Insert_LargeFlexibilityDays_ShouldInsert()
    {
        // Arrange
        var query = CreateQuery("ZRH", "JFK", 30);

        // Act
        _repository.Insert(query);

        // Assert
        var allQueries = _repository.GetAll();
        var insertedQuery = Assert.Single(allQueries);
        Assert.Equal(30, insertedQuery.FlexibilityDays);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_SingleQuery_ShouldReturnOne()
    {
        // Arrange
        var query = CreateQuery("ZRH", "JFK", 3);
        _repository.Insert(query);

        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public void GetAll_MultipleQueries_ShouldReturnAll()
    {
        // Arrange
        _repository.Insert(CreateQuery("ZRH", "JFK", 3));
        _repository.Insert(CreateQuery("GVA", "LHR", 5));
        _repository.Insert(CreateQuery("ZRH", "BCN", 0));

        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void GetAll_ShouldReturnQueriesWithAllProperties()
    {
        // Arrange
        var expectedDate = new DateTime(2026, 3, 15);
        var query = CreateQuery("ZRH", "JFK", 7, expectedDate);
        _repository.Insert(query);

        // Act
        var result = _repository.GetAll();

        // Assert
        var returnedQuery = Assert.Single(result);
        Assert.Equivalent(returnedQuery, query);
    }

    [Fact]
    public void GetAll_ShouldReturnQueriesWithGeneratedIds()
    {
        // Arrange
        _repository.Insert(CreateQuery("ZRH", "JFK", 3));
        _repository.Insert(CreateQuery("GVA", "LHR", 5));

        // Act
        var result = _repository.GetAll();

        // Assert
        Assert.All(result, q => Assert.NotEqual(ObjectId.Empty, q.Id));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void InsertAndGetAll_ShouldWorkTogether()
    {
        // Arrange
        var query = CreateQuery("ZRH", "JFK", 5, new DateTime(2026, 4, 20));

        // Act
        _repository.Insert(query);
        var result = _repository.GetAll();

        // Assert
        var retrievedQuery = Assert.Single(result);
        Assert.Equivalent(query, retrievedQuery);
    }

    [Fact]
    public void MultipleInserts_GetAll_ShouldReturnCorrectCount()
    {
        // Arrange & Act
        for (int i = 0; i < 10; i++)
        {
            _repository.Insert(CreateQuery($"OR{i}", $"DE{i}", i));
        }

        var result = _repository.GetAll();

        // Assert
        Assert.Equal(10, result.Count);
    }

    #endregion

    #region Helper Methods

    private QueryEntity CreateQuery(string origin, string destination, int flexDays, DateTime? anchorDate = null)
    {
        return new QueryEntity
        {
            OriginIata = origin,
            DestinationIata = destination,
            FlexibilityDays = flexDays,
            AnchorDate = anchorDate ?? DateTime.UtcNow
        };
    }

    #endregion
}
