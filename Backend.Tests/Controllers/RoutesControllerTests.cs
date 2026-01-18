using FlightTracker.Api.Controllers;
using FlightTracker.Api.Services;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Contracts;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

public class RoutesControllerTests
{
    private readonly Mock<IFlightRepository> _mockFlightRepo;
    private readonly Mock<IObservationRepository> _mockObservationRepo;
    private readonly Mock<IFlightStatsService> _mockStatsService;
    private readonly RoutesController _controller;

    public RoutesControllerTests()
    {
        _mockFlightRepo = new Mock<IFlightRepository>();
        _mockObservationRepo = new Mock<IObservationRepository>();
        _mockStatsService = new Mock<IFlightStatsService>();

        _controller = new RoutesController(
            _mockFlightRepo.Object,
            _mockObservationRepo.Object,
            _mockStatsService.Object);
    }

    #region GetFlexStats - Validation Tests

    [Fact]
    public void GetFlexStats_MissingTargetDate_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "", 3);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("target_date is required", badRequestResult.Value);
    }

    [Fact]
    public void GetFlexStats_NullTargetDate_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", null!, 3);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("target_date is required", badRequestResult.Value);
    }

    [Fact]
    public void GetFlexStats_WhitespaceTargetDate_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "   ", 3);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("target_date is required", badRequestResult.Value);
    }

    [Fact]
    public void GetFlexStats_InvalidDateFormat_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026/01/15", 3);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid target_date format. Use YYYY-MM-DD.", badRequestResult.Value);
    }

    [Fact]
    public void GetFlexStats_InvalidDateFormat_USStyle_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "01-15-2026", 3);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid target_date format. Use YYYY-MM-DD.", badRequestResult.Value);
    }

    [Fact]
    public void GetFlexStats_NegativeFlexDays_ShouldReturnBadRequest()
    {
        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", -1);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("flex_days must be non-negative", badRequestResult.Value);
    }

    #endregion

    #region GetFlexStats - Success Tests

    [Fact]
    public void GetFlexStats_NoFlightsFound_ShouldReturnEmptyResponse()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", It.IsAny<DateTime>(), null))
            .Returns(new List<FlightEntity>());

        var expected = new FlexStatsResponseDto
        {
            Origin = "ZRH",
            Destination = "JFK",
            TargetDate = "2026-01-15",
            FlexDays = 2,
            Timezone = "Europe/Zurich",
            Series = new List<FlexStatsBucketDto>(),
            Best = null
        };

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equivalent(expected, okResult.Value);
    }

    [Fact]
    public void GetFlexStats_ZeroFlexDays_ShouldQuerySingleDate()
    {
        // Arrange
        var targetDate = new DateTime(2026, 1, 15);
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", targetDate, null))
            .Returns(new List<FlightEntity>());

        // Act
        _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 0);

        // Assert
        _mockFlightRepo.Verify(r => r.Query("ZRH", "JFK", targetDate, null), Times.Once);
    }

    [Fact]
    public void GetFlexStats_SingleFlightWithObservations_ShouldReturnCorrectly()
    {
        // Arrange
        var flight = CreateTestFlight("LX123", new DateTime(2026, 1, 15), "ZRH", "JFK");
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, 100m),
            CreateObservation(flight.Id, 200m),
            CreateObservation(flight.Id, 150m)
        };

        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null))
            .Returns(new List<FlightEntity> { flight });
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight.Id.ToString()))
            .Returns(observations);
        _mockStatsService.Setup(s => s.Median(It.IsAny<List<decimal>>()))
            .Returns(150m);

        var expected = new FlexStatsResponseDto
        {
            Origin = "ZRH",
            Destination = "JFK",
            TargetDate = "2026-01-15",
            FlexDays = 0,
            Timezone = "Europe/Zurich",
            Series = new List<FlexStatsBucketDto>
            {
                new FlexStatsBucketDto
                {
                    DepartureDate = "2026-01-15",
                    MedianPriceChf = 150m,
                    FlightId = flight.Id.ToString()
                }
            },
            Best = new FlexStatsBucketDto
            {
                DepartureDate = "2026-01-15",
                MedianPriceChf = 150m,
                FlightId = flight.Id.ToString()
            }
        };

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equivalent(expected, okResult.Value);
    }

    [Fact]
    public void GetFlexStats_MultipleFlightsSameDate_ShouldPickLowestMedian()
    {
        // Arrange
        var flight1 = CreateTestFlight("LX123", new DateTime(2026, 1, 15), "ZRH", "JFK");
        var flight2 = CreateTestFlight("LX456", new DateTime(2026, 1, 15), "ZRH", "JFK");

        var observations1 = new List<ObservationEntity> { CreateObservation(flight1.Id, 200m) };
        var observations2 = new List<ObservationEntity> { CreateObservation(flight2.Id, 150m) };

        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null))
            .Returns(new List<FlightEntity> { flight1, flight2 });
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight1.Id.ToString()))
            .Returns(observations1);
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight2.Id.ToString()))
            .Returns(observations2);
        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Contains(200m))))
            .Returns(200m);
        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Contains(150m))))
            .Returns(150m);

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FlexStatsResponseDto>(okResult.Value);
        Assert.Single(response.Series);
        Assert.Equal(150m, response.Series[0].MedianPriceChf);
    }

    [Fact]
    public void GetFlexStats_FlightWithNoObservations_ShouldBeSkipped()
    {
        // Arrange
        var flight = CreateTestFlight("LX123", new DateTime(2026, 1, 15), "ZRH", "JFK");

        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null))
            .Returns(new List<FlightEntity> { flight });
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight.Id.ToString()))
            .Returns(new List<ObservationEntity>());
        _mockStatsService.Setup(s => s.Median(It.IsAny<List<decimal>>()))
            .Returns((decimal?)null);

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FlexStatsResponseDto>(okResult.Value);
        Assert.Empty(response.Series);
    }

    [Fact]
    public void GetFlexStats_MultipleFlightsOneWithoutObservations_ShouldReturnOnlyFlightWithObservations()
    {
        // Arrange
        var flight1 = CreateTestFlight("LX123", new DateTime(2026, 1, 15), "ZRH", "JFK");
        var flight2 = CreateTestFlight("LX456", new DateTime(2026, 1, 15), "ZRH", "JFK");

        var observations2 = new List<ObservationEntity> { CreateObservation(flight2.Id, 150m) };

        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null))
            .Returns(new List<FlightEntity> { flight1, flight2 });
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight1.Id.ToString()))
            .Returns(new List<ObservationEntity>());
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight2.Id.ToString()))
            .Returns(observations2);
        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Count == 0)))
            .Returns((decimal?)null);
        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Contains(150m))))
            .Returns(150m);

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FlexStatsResponseDto>(okResult.Value);
        Assert.Equal(flight2.Id.ToString(), response.Series[0].FlightId);
    }

    [Fact]
    public void GetFlexStats_WithFlexDays_ShouldQueryAllDatesInRange()
    {
        // Arrange
        var targetDate = new DateTime(2026, 1, 15);
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", It.IsAny<DateTime>(), null))
            .Returns(new List<FlightEntity>());

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 2);

        // Assert - Should query for dates: 2026-01-13, 14, 15, 16, 17 (5 days)
        _mockFlightRepo.Verify(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 13), null), Times.Once);
        _mockFlightRepo.Verify(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 14), null), Times.Once);
        _mockFlightRepo.Verify(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null), Times.Once);
        _mockFlightRepo.Verify(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 16), null), Times.Once);
        _mockFlightRepo.Verify(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 17), null), Times.Once);
    }

    [Fact]
    public void GetFlexStats_MultipleDatesWithFlights_ShouldReturnBestOverall()
    {
        // Arrange
        var flight1 = CreateTestFlight("LX123", new DateTime(2026, 1, 14), "ZRH", "JFK");
        var flight2 = CreateTestFlight("LX456", new DateTime(2026, 1, 15), "ZRH", "JFK");
        var flight3 = CreateTestFlight("LX789", new DateTime(2026, 1, 16), "ZRH", "JFK");

        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 14), null))
            .Returns(new List<FlightEntity> { flight1 });
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null))
            .Returns(new List<FlightEntity> { flight2 });
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 16), null))
            .Returns(new List<FlightEntity> { flight3 });

        _mockObservationRepo.Setup(r => r.GetByFlightId(flight1.Id.ToString()))
            .Returns(new List<ObservationEntity> { CreateObservation(flight1.Id, 200m) });
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight2.Id.ToString()))
            .Returns(new List<ObservationEntity> { CreateObservation(flight2.Id, 100m) });
        _mockObservationRepo.Setup(r => r.GetByFlightId(flight3.Id.ToString()))
            .Returns(new List<ObservationEntity> { CreateObservation(flight3.Id, 150m) });

        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Contains(200m))))
            .Returns(200m);
        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Contains(100m))))
            .Returns(100m);
        _mockStatsService.Setup(s => s.Median(It.Is<List<decimal>>(l => l.Contains(150m))))
            .Returns(150m);

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FlexStatsResponseDto>(okResult.Value);
        Assert.Equal(3, response.Series.Count);
        Assert.Equal(100m, response.Best!.MedianPriceChf);
    }

    [Fact]
    public void GetFlexStats_SeriesShouldBeOrderedByDate()
    {
        // Arrange
        var flight1 = CreateTestFlight("LX123", new DateTime(2026, 1, 16), "ZRH", "JFK");
        var flight2 = CreateTestFlight("LX456", new DateTime(2026, 1, 14), "ZRH", "JFK");
        var flight3 = CreateTestFlight("LX789", new DateTime(2026, 1, 15), "ZRH", "JFK");

        // Setup in non-chronological order
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 14), null))
            .Returns(new List<FlightEntity> { flight2 });
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 15), null))
            .Returns(new List<FlightEntity> { flight3 });
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", new DateTime(2026, 1, 16), null))
            .Returns(new List<FlightEntity> { flight1 });

        _mockObservationRepo.Setup(r => r.GetByFlightId(It.IsAny<string>()))
            .Returns((string id) => new List<ObservationEntity>
            {
                CreateObservation(new ObjectId(id), 100m)
            });

        _mockStatsService.Setup(s => s.Median(It.IsAny<List<decimal>>()))
            .Returns(100m);

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FlexStatsResponseDto>(okResult.Value);
        Assert.Equal("2026-01-14", response.Series[0].DepartureDate);
        Assert.Equal("2026-01-16", response.Series[2].DepartureDate);
    }

    [Fact]
    public void GetFlexStats_ShouldSetCorrectResponseMetadata()
    {
        // Arrange
        _mockFlightRepo.Setup(r => r.Query("ZRH", "JFK", It.IsAny<DateTime>(), null))
            .Returns(new List<FlightEntity>());

        // Act
        var result = _controller.GetFlexStats("ZRH", "JFK", "2026-01-15", 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<FlexStatsResponseDto>(okResult.Value);
        Assert.Equal("ZRH", response.Origin);
        Assert.Equal(3, response.FlexDays);
    }

    #endregion

    #region Helper Methods

    private FlightEntity CreateTestFlight(string flightNumber, DateTime departureDate, string origin, string destination)
    {
        return new FlightEntity
        {
            Id = ObjectId.NewObjectId(),
            FlightNumber = flightNumber,
            DepartureDate = departureDate,
            OriginIata = origin,
            DestinationIata = destination
        };
    }

    private ObservationEntity CreateObservation(ObjectId flightId, decimal price)
    {
        return new ObservationEntity
        {
            Id = ObjectId.NewObjectId(),
            FlightId = flightId,
            ObservedAtUtc = DateTime.UtcNow,
            PriceChf = price
        };
    }

    #endregion
}
