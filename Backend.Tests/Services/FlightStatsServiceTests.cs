using FlightTracker.Api.Services;
using FlightTracker.Api.Storage.Entities;
using LiteDB;
using Xunit;

namespace Backend.Tests.Services;

public class FlightStatsServiceTests
{
    private readonly FlightStatsService _service;

    public FlightStatsServiceTests()
    {
        _service = new FlightStatsService();
    }

    #region Timezone and Date Conversion Tests

    [Fact]
    public void ConvertUtcToZurich_ShouldConvertCorrectly_WinterTime()
    {
        // Arrange - January is winter time (UTC+1)
        var utc = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.ConvertUtcToZurich(utc);

        // Assert
        Assert.Equal(11, result.Hour); // UTC+1 in winter
    }

    [Fact]
    public void ConvertUtcToZurich_ShouldConvertCorrectly_SummerTime()
    {
        // Arrange - July is summer time (UTC+2)
        var utc = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.ConvertUtcToZurich(utc);

        // Assert
        Assert.Equal(12, result.Hour); // UTC+2 in summer
    }

    [Fact]
    public void GetBookingDateLocal_ShouldReturnCorrectDate()
    {
        // Arrange - Late evening UTC should become next day in Zurich
        var utc = new DateTime(2026, 1, 15, 23, 30, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetBookingDateLocal(utc);

        // Assert
        Assert.Equal(new DateOnly(2026, 1, 16), result);
    }

    #endregion

    #region Weekday Tests

    [Theory]
    [InlineData("2026-01-12", 1)] // Monday
    [InlineData("2026-01-13", 2)] // Tuesday
    [InlineData("2026-01-14", 3)] // Wednesday
    [InlineData("2026-01-15", 4)] // Thursday
    [InlineData("2026-01-16", 5)] // Friday
    [InlineData("2026-01-17", 6)] // Saturday
    [InlineData("2026-01-18", 7)] // Sunday
    public void GetWeekdayLocal_ShouldReturnCorrectWeekday(string dateStr, int expectedWeekday)
    {
        // Arrange - midday UTC to avoid timezone edge cases
        var date = DateTime.Parse(dateStr);
        var utc = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.GetWeekdayLocal(utc);

        // Assert
        Assert.Equal(expectedWeekday, result);
    }

    [Theory]
    [InlineData(1, "Mon")]
    [InlineData(2, "Tue")]
    [InlineData(3, "Wed")]
    [InlineData(4, "Thu")]
    [InlineData(5, "Fri")]
    [InlineData(6, "Sat")]
    [InlineData(7, "Sun")]
    [InlineData(0, "")]
    [InlineData(8, "")]
    public void GetWeekdayLabel_ShouldReturnCorrectLabel(int weekday, string expected)
    {
        // Act
        var result = _service.GetWeekdayLabel(weekday);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Days to Departure Tests

    [Fact]
    public void GetDaysToDeparture_ShouldCalculateCorrectly()
    {
        // Arrange
        var observedAtUtc = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        var departureDate = new DateTime(2026, 1, 20, 0, 0, 0);

        // Act
        var result = _service.GetDaysToDeparture(observedAtUtc, departureDate);

        // Assert
        Assert.Equal(10, result);
    }

    [Fact]
    public void GetDaysToDeparture_SameDayBooking_ShouldReturnZero()
    {
        // Arrange
        var observedAtUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var departureDate = new DateTime(2026, 1, 15, 0, 0, 0);

        // Act
        var result = _service.GetDaysToDeparture(observedAtUtc, departureDate);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetDaysToDeparture_AfterDeparture_ShouldReturnNegative()
    {
        // Arrange
        var observedAtUtc = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        var departureDate = new DateTime(2026, 1, 15, 0, 0, 0);

        // Act
        var result = _service.GetDaysToDeparture(observedAtUtc, departureDate);

        // Assert
        Assert.Equal(-5, result);
    }

    #endregion

    #region Statistics Aggregation Tests

    [Fact]
    public void Median_OddNumberOfValues_ShouldReturnMiddleValue()
    {
        // Arrange
        var values = new List<decimal> { 100m, 200m, 300m, 400m, 500m };

        // Act
        var result = _service.Median(values);

        // Assert
        Assert.Equal(300m, result);
    }

    [Fact]
    public void Median_EvenNumberOfValues_ShouldReturnAverage()
    {
        // Arrange
        var values = new List<decimal> { 100m, 200m, 300m, 400m };

        // Act
        var result = _service.Median(values);

        // Assert
        Assert.Equal(250m, result);
    }

    [Fact]
    public void Median_UnsortedValues_ShouldSortAndCalculate()
    {
        // Arrange
        var values = new List<decimal> { 500m, 100m, 300m, 200m, 400m };

        // Act
        var result = _service.Median(values);

        // Assert
        Assert.Equal(300m, result);
    }

    [Fact]
    public void Median_SingleValue_ShouldReturnThatValue()
    {
        // Arrange
        var values = new List<decimal> { 250m };

        // Act
        var result = _service.Median(values);

        // Assert
        Assert.Equal(250m, result);
    }

    [Fact]
    public void Median_EmptyList_ShouldReturnNull()
    {
        // Arrange
        var values = new List<decimal>();

        // Act
        var result = _service.Median(values);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void AggregateStats_WithValues_ShouldCalculateCorrectly()
    {
        // Arrange
        var prices = new List<decimal> { 100m, 200m, 300m, 400m, 500m };

        // Act
        var result = _service.AggregateStats(prices);

        // Assert
        Assert.Equal(100m, result.Min);
        Assert.Equal(500m, result.Max);
        Assert.Equal(300m, result.Avg);
        Assert.Equal(300m, result.Median);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void AggregateStats_EmptyList_ShouldReturnNulls()
    {
        // Arrange
        var prices = new List<decimal>();

        // Act
        var result = _service.AggregateStats(prices);

        // Assert
        Assert.Null(result.Min);
        Assert.Null(result.Max);
        Assert.Null(result.Avg);
        Assert.Null(result.Median);
        Assert.Equal(0, result.Count);
    }

    #endregion

    #region ComputeWeekdayStats Tests

    [Fact]
    public void ComputeWeekdayStats_ShouldGroupByWeekday()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            // Monday observations (2026-01-12)
            CreateObservation(flight.Id, new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc), 150m),
            // Tuesday observation (2026-01-13)
            CreateObservation(flight.Id, new DateTime(2026, 1, 13, 10, 0, 0, DateTimeKind.Utc), 200m),
        };

        // Act
        var result = _service.ComputeWeekdayStats(flight, observations);

        // Assert
        Assert.Equal(7, result.Series.Count); // All 7 days should be present

        var monday = result.Series.First(s => s.Weekday == 1);
        Assert.Equal("Mon", monday.Label);
        Assert.Equal(2, monday.Count);
        Assert.Equal(100m, monday.Min);
        Assert.Equal(150m, monday.Max);
        Assert.Equal(125m, monday.Avg);

        var tuesday = result.Series.First(s => s.Weekday == 2);
        Assert.Equal("Tue", tuesday.Label);
        Assert.Equal(1, tuesday.Count);
        Assert.Equal(200m, tuesday.Min);
    }

    [Fact]
    public void ComputeWeekdayStats_EmptyObservations_ShouldReturnAllWeekdaysWithZeroCounts()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();

        // Act
        var result = _service.ComputeWeekdayStats(flight, observations);

        // Assert
        Assert.Equal(7, result.Series.Count);
        Assert.All(result.Series, bucket => Assert.Equal(0, bucket.Count));
        Assert.All(result.Series, bucket => Assert.Null(bucket.Min));
    }

    #endregion

    #region ComputeBookingDateStats Tests

    [Fact]
    public void ComputeBookingDateStats_ShouldGroupByDate()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 14, 0, 0, DateTimeKind.Utc), 120m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc), 150m),
        };

        // Act
        var result = _service.ComputeBookingDateStats(flight, observations);

        // Assert
        Assert.Equal(2, result.Series.Count);

        var firstDay = result.Series[0];
        Assert.Equal("2026-01-10", firstDay.Date);
        Assert.Equal(2, firstDay.Count);
        Assert.Equal(100m, firstDay.Min);
        Assert.Equal(120m, firstDay.Max);
        Assert.Equal(110m, firstDay.Avg);

        var secondDay = result.Series[1];
        Assert.Equal("2026-01-11", secondDay.Date);
        Assert.Equal(1, secondDay.Count);
        Assert.Equal(150m, secondDay.Min);
    }

    [Fact]
    public void ComputeBookingDateStats_ShouldOrderByDate()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), 300m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc), 200m),
        };

        // Act
        var result = _service.ComputeBookingDateStats(flight, observations);

        // Assert
        Assert.Equal(3, result.Series.Count);
        Assert.Equal("2026-01-10", result.Series[0].Date);
        Assert.Equal("2026-01-12", result.Series[1].Date);
        Assert.Equal("2026-01-15", result.Series[2].Date);
    }

    #endregion

    #region ComputeDaysToDepartureStats Tests

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket1_ShouldGroupByDay()
    {
        // Arrange
        var flight = CreateTestFlight();
        flight.DepartureDate = new DateTime(2026, 1, 20);
        var observations = new List<ObservationEntity>
        {
            // 10 days before
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m),
            // 9 days before
            CreateObservation(flight.Id, new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), 110m),
            // 5 days before
            CreateObservation(flight.Id, new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc), 150m),
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 1);

        // Assert
        Assert.Equal(3, result.Series.Count);
        Assert.Equal(5, result.Series[0].DaysFrom);
        Assert.Equal(6, result.Series[0].DaysTo);
        Assert.Equal(9, result.Series[1].DaysFrom);
        Assert.Equal(10, result.Series[1].DaysTo);
        Assert.Equal(10, result.Series[2].DaysFrom);
        Assert.Equal(11, result.Series[2].DaysTo);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket7_ShouldGroupByWeek()
    {
        // Arrange
        var flight = CreateTestFlight();
        flight.DepartureDate = new DateTime(2026, 1, 30);
        var observations = new List<ObservationEntity>
        {
            // 20 days before (bucket 14-21)
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), 110m),
            // 10 days before (bucket 7-14)
            CreateObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 150m),
            // 5 days before (bucket 0-7)
            CreateObservation(flight.Id, new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc), 200m),
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 7);

        // Assert
        Assert.Equal(3, result.Series.Count);

        var firstBucket = result.Series[0];
        Assert.Equal(0, firstBucket.DaysFrom);
        Assert.Equal(7, firstBucket.DaysTo);
        Assert.Equal(1, firstBucket.Count);

        var secondBucket = result.Series[1];
        Assert.Equal(7, secondBucket.DaysFrom);
        Assert.Equal(14, secondBucket.DaysTo);
        Assert.Equal(1, secondBucket.Count);

        var thirdBucket = result.Series[2];
        Assert.Equal(14, thirdBucket.DaysFrom);
        Assert.Equal(21, thirdBucket.DaysTo);
        Assert.Equal(2, thirdBucket.Count);
        Assert.Equal(105m, thirdBucket.Avg);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_ShouldExcludeNegativeDays()
    {
        // Arrange
        var flight = CreateTestFlight();
        flight.DepartureDate = new DateTime(2026, 1, 15);
        var observations = new List<ObservationEntity>
        {
            // After departure (negative days) - should be excluded
            CreateObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 999m),
            // Before departure
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m),
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 1);

        // Assert
        Assert.Equal(1, result.Series.Count);
        Assert.Equal(5, result.Series[0].DaysFrom);
        Assert.Equal(100m, result.Series[0].Min);
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
