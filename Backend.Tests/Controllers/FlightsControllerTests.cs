using FlightTracker.Api.Controllers;
using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Services;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Contracts;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

public class FlightsControllerTests
{
    private readonly Mock<IFlightRepository> _mockFlightRepo;
    private readonly Mock<IObservationRepository> _mockObservationRepo;
    private readonly Mock<IFlightStatsService> _mockStatsService;
    private readonly Mock<IQueryRepository> _mockQueryRepo;
    private readonly FlightsController _controller;

    public FlightsControllerTests()
    {
        _mockFlightRepo = new Mock<IFlightRepository>();
        _mockObservationRepo = new Mock<IObservationRepository>();
        _mockStatsService = new Mock<IFlightStatsService>();
        _mockQueryRepo = new Mock<IQueryRepository>();

        _controller = new FlightsController(
            _mockFlightRepo.Object,
            _mockObservationRepo.Object,
            _mockStatsService.Object,
            _mockQueryRepo.Object);
    }

    #region Get (Query) Tests

    [Fact]
    public void Get_NoParameters_ShouldReturnAllFlights()
    {
        // Arrange
        var flights = new List<FlightEntity> { CreateTestFlight(), CreateTestFlight() };
        var dtos = new List<FlightResponseDto> { new FlightResponseDto(), new FlightResponseDto() };

        _mockFlightRepo.Setup(r => r.Query(null, null, null, null)).Returns(flights);
        _mockStatsService.Setup(s => s.ToFlightResponseDto(It.IsAny<FlightEntity>()))
            .Returns((FlightEntity f) => new FlightResponseDto { Id = f.Id.ToString() });

        // Act
        var result = _controller.Get();

        // Assert
        // intermediary asserts are needed to unwrap ActionResult<>
        var okResult = Assert.IsType<ActionResult<List<FlightResponseDto>>>(result);
        var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
        var resultDtos = Assert.IsType<List<FlightResponseDto>>(returnValue.Value);
        Assert.Equal(2, resultDtos.Count);
    }

    [Fact]
    public void Get_WithOriginFilter_ShouldPassToRepository()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.Query("ZRH", null, null, null))
            .Returns(new List<FlightEntity>());
        _mockStatsService.Setup(s => s.ToFlightResponseDto(It.IsAny<FlightEntity>()))
            .Returns(new FlightResponseDto());

        // Act
        var result = _controller.Get(origin: "ZRH");

        // Assert
        _mockFlightRepo.Verify(r => r.Query("ZRH", null, null, null), Times.Once);
    }

    [Fact]
    public void Get_WithInvalidDateFormat_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.Get(departure_date: "invalid-date");

        // Assert
        var badRequestResult = Assert.IsType<ActionResult<List<FlightResponseDto>>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal("Invalid departure_date format. Use YYYY-MM-DD.", badRequest.Value);
    }

    [Fact]
    public void Get_WithValidDate_ShouldParseAndPassToRepository()
    {
        // Arrange
        var expectedDate = new DateTime(2026, 1, 15);
        _mockFlightRepo.Setup(r => r.Query(null, null, expectedDate, null))
            .Returns(new List<FlightEntity>());
        _mockStatsService.Setup(s => s.ToFlightResponseDto(It.IsAny<FlightEntity>()))
            .Returns(new FlightResponseDto());

        // Act
        var result = _controller.Get(departure_date: "2026-01-15");

        // Assert
        _mockFlightRepo.Verify(r => r.Query(null, null, expectedDate, null), Times.Once);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public void GetById_ExistingFlight_ShouldReturnFlight()
    {
        // Arrange
        var flight = CreateTestFlight();
        var dto = new FlightResponseDto { Id = flight.Id.ToString() };

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockStatsService.Setup(s => s.ToFlightResponseDto(flight)).Returns(dto);

        // Act
        var result = _controller.GetById(flight.Id.ToString());

        // Assert
        var okResult = Assert.IsType<ActionResult<FlightResponseDto>>(result);
        var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
        var resultDto = Assert.IsType<FlightResponseDto>(returnValue.Value);
        Assert.Equal(flight.Id.ToString(), resultDto.Id);
    }

    [Fact]
    public void GetById_NonExistentFlight_ShouldReturnNotFound()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((FlightEntity)null);

        // Act
        var result = _controller.GetById("nonexistent-id");

        // Assert
        var notFoundResult = Assert.IsType<ActionResult<FlightResponseDto>>(result);
        Assert.IsType<NotFoundResult>(notFoundResult.Result);
    }

    #endregion

    #region GetObservations Tests

    [Fact]
    public void GetObservations_ValidFlightId_ShouldReturnObservations()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), 110m)
        };

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightIdWithDateFilter(flight.Id.ToString(), null, null))
            .Returns(observations);
        _mockStatsService.Setup(s => s.ConvertUtcToZurich(It.IsAny<DateTime>()))
            .Returns((DateTime dt) => dt.AddHours(1));

        // Act
        var result = _controller.GetObservations(flight.Id.ToString());

        // Assert
        var okResult = Assert.IsType<ActionResult<List<ObservationResponseDto>>>(result);
        var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
        var resultDtos = Assert.IsType<List<ObservationResponseDto>>(returnValue.Value);
        Assert.Equal(2, resultDtos.Count);
    }

    [Fact]
    public void GetObservations_NonExistentFlight_ShouldReturnNotFound()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((FlightEntity)null);

        // Act
        var result = _controller.GetObservations("nonexistent-id");

        // Assert
        var notFoundResult = Assert.IsType<ActionResult<List<ObservationResponseDto>>>(result);
        var notFound = Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
        Assert.Equal("Flight not found", notFound.Value);
    }

    [Fact]
    public void GetObservations_WithInvalidFromDate_ShouldReturnBadRequest()
    {
        // Arrange
        var flight = CreateTestFlight();
        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);

        // Act
        var result = _controller.GetObservations(flight.Id.ToString(), from: "invalid-date");

        // Assert
        var badRequestResult = Assert.IsType<ActionResult<List<ObservationResponseDto>>>(result);
        var badRequest = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Contains("Invalid from format. Use YYYY-MM-DD.", badRequest.Value.ToString());
    }

    [Fact]
    public void GetObservations_WithValidDateRange_ShouldFilterCorrectly()
    {
        // Arrange
        var flight = CreateTestFlight();
        var fromDate = DateTime.SpecifyKind(new DateTime(2026, 1, 10), DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(new DateTime(2026, 1, 16), DateTimeKind.Utc); // End of day

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightIdWithDateFilter(
                flight.Id.ToString(),
                fromDate,
                toDate))
            .Returns(new List<ObservationEntity>());
        _mockStatsService.Setup(s => s.ConvertUtcToZurich(It.IsAny<DateTime>()))
            .Returns((DateTime dt) => dt);

        // Act
        var result = _controller.GetObservations(
            flight.Id.ToString(),
            from: "2026-01-10",
            to: "2026-01-15");

        // Assert
        _mockObservationRepo.Verify(r => r.GetByFlightIdWithDateFilter(
            flight.Id.ToString(),
            fromDate,
            toDate), Times.Once);
    }

    #endregion

    #region GetWeekdayStats Tests

    [Fact]
    public void GetWeekdayStats_ValidFlightId_ShouldReturnStats()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();
        var statsResponse = new WeekdayStatsResponseDto();

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight.Id.ToString())).Returns(observations);
        _mockStatsService.Setup(s => s.ComputeWeekdayStats(flight, observations)).Returns(statsResponse);

        // Act
        var result = _controller.GetWeekdayStats(flight.Id.ToString());

        // Assert
        var okResult = Assert.IsType<ActionResult<WeekdayStatsResponseDto>>(result);
        var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
        Assert.Same(statsResponse, returnValue.Value);
    }

    [Fact]
    public void GetWeekdayStats_NonExistentFlight_ShouldReturnNotFound()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((FlightEntity)null);

        // Act
        var result = _controller.GetWeekdayStats("nonexistent-id");

        // Assert
        var notFoundResult = Assert.IsType<ActionResult<WeekdayStatsResponseDto>>(result);
        var notFound = Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
        Assert.Contains("Flight not found", notFound.Value.ToString());
    }

    #endregion

    #region GetBookingDateStats Tests

    [Fact]
    public void GetBookingDateStats_ValidFlightId_ShouldReturnStats()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();
        var statsResponse = new BookingDateStatsResponseDto();

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightIdWithDateFilter(flight.Id.ToString(), null, null))
            .Returns(observations);
        _mockStatsService.Setup(s => s.ComputeBookingDateStats(flight, observations))
            .Returns(statsResponse);

        // Act
        var result = _controller.GetBookingDateStats(flight.Id.ToString());

        // Assert
        var okResult = Assert.IsType<ActionResult<BookingDateStatsResponseDto>>(result);
        var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
        Assert.Same(statsResponse, returnValue.Value);
    }

    [Fact]
    public void GetBookingDateStats_WithDateFilters_ShouldPassToRepository()
    {
        // Arrange
        var flight = CreateTestFlight();
        var fromDate = DateTime.SpecifyKind(new DateTime(2026, 1, 10), DateTimeKind.Utc);
        var toDate = DateTime.SpecifyKind(new DateTime(2026, 1, 16), DateTimeKind.Utc);

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightIdWithDateFilter(
                flight.Id.ToString(),
                fromDate,
                toDate))
            .Returns(new List<ObservationEntity>());
        _mockStatsService.Setup(s => s.ComputeBookingDateStats(It.IsAny<FlightEntity>(), It.IsAny<List<ObservationEntity>>()))
            .Returns(new BookingDateStatsResponseDto());

        // Act
        var result = _controller.GetBookingDateStats(
            flight.Id.ToString(),
            from: "2026-01-10",
            to: "2026-01-15");

        // Assert
        _mockObservationRepo.Verify(r => r.GetByFlightIdWithDateFilter(
            flight.Id.ToString(),
            fromDate,
            toDate), Times.Once);
    }

    #endregion

    #region GetDaysToDepartureStats Tests

    [Fact]
    public void GetDaysToDepartureStats_ValidBucket1_ShouldReturnStats()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();
        var statsResponse = new DaysToDepartureStatsResponseDto();

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight.Id.ToString())).Returns(observations);
        _mockStatsService.Setup(s => s.ComputeDaysToDepartureStats(flight, observations, 1))
            .Returns(statsResponse);

        // Act
        var result = _controller.GetDaysToDepartureStats(flight.Id.ToString(), bucket: 1);

        // Assert
        var okResult = Assert.IsType<ActionResult<DaysToDepartureStatsResponseDto>>(result);
        var returnValue = Assert.IsType<OkObjectResult>(okResult.Result);
        Assert.Same(statsResponse, returnValue.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void GetDaysToDepartureStats_ValidBuckets_ShouldAccept(int bucket)
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();
        var statsResponse = new DaysToDepartureStatsResponseDto();

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight.Id.ToString())).Returns(observations);
        _mockStatsService.Setup(s => s.ComputeDaysToDepartureStats(flight, observations, bucket))
            .Returns(statsResponse);

        // Act
        var result = _controller.GetDaysToDepartureStats(flight.Id.ToString(), bucket: bucket);

        // Assert
        var okResult = Assert.IsType<ActionResult<DaysToDepartureStatsResponseDto>>(result);
        Assert.IsType<OkObjectResult>(okResult.Result);
    }

    [Fact]
    public void GetDaysToDepartureStats_NonExistentFlight_ShouldReturnNotFound()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.GetById(It.IsAny<string>())).Returns((FlightEntity)null);

        // Act
        var result = _controller.GetDaysToDepartureStats("nonexistent-id", bucket: 1);

        // Assert
        var notFoundResult = Assert.IsType<ActionResult<DaysToDepartureStatsResponseDto>>(result);
        var notFound = Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
        Assert.Contains("Flight not found", notFound.Value.ToString());
    }

    [Fact]
    public void GetDaysToDepartureStats_DefaultBucket_ShouldBe1()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();
        var statsResponse = new DaysToDepartureStatsResponseDto();

        _mockFlightRepo.Setup(r => r.GetById(flight.Id.ToString())).Returns(flight);
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight.Id.ToString())).Returns(observations);
        _mockStatsService.Setup(s => s.ComputeDaysToDepartureStats(flight, observations, 1))
            .Returns(statsResponse);

        // Act - Not providing bucket parameter
        var result = _controller.GetDaysToDepartureStats(flight.Id.ToString());

        // Assert
        _mockStatsService.Verify(s => s.ComputeDaysToDepartureStats(flight, observations, 1), Times.Once);
    }

    #endregion

    #region Helper Methods

    private FlightEntity CreateTestFlight()
    {
        return new FlightEntity
        {
            Id = ObjectId.NewObjectId(),
            FlightNumber = "LX123",
            DepartureDate = new DateTime(2026, 2, 1, 10, 30, 0),
            OriginIata = "ZRH",
            DestinationIata = "JFK"
        };
    }

    private ObservationEntity CreateObservation(ObjectId flightId, DateTime observedAtUtc, decimal price)
    {
        return new ObservationEntity
        {
            Id = ObjectId.NewObjectId(),
            FlightId = flightId,
            ObservedAtUtc = observedAtUtc,
            PriceChf = price
        };
    }

    #endregion
}
