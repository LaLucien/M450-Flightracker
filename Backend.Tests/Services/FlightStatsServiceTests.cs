using FlightTracker.Api.Services;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Contracts;
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
    public void ConvertUtcToZurich_WinterTime_ShouldConvertCorrectly()
    {
        // Arrange - January is winter time (UTC+1)
        var utc = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        // Act
        var result = _service.ConvertUtcToZurich(utc);

        // Assert
        Assert.Equal(11, result.Hour); // UTC+1 in winter
    }

    [Fact]
    public void ConvertUtcToZurich_SummerTime_ShouldConvertCorrectly()
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
        var expected = new StatsAggregateDto
        {
            Min = 100m,
            Max = 500m,
            Avg = 300m,
            Median = 300m,
            Count = 5
        };

        // Act
        var result = _service.AggregateStats(prices);

        // Assert
        Assert.Equivalent(expected, result);
    }

    [Fact]
    public void AggregateStats_EmptyList_ShouldReturnNulls()
    {
        // Arrange
        var prices = new List<decimal>();
        var expected = new StatsAggregateDto
        {
            Min = null,
            Max = null,
            Avg = null,
            Median = null,
            Count = 0
        };

        // Act
        var result = _service.AggregateStats(prices);

        // Assert
        Assert.Equivalent(expected, result);
    }

    #endregion

    #region ComputeWeekdayStats Tests

    [Fact]
    public void ComputeWeekdayStats_ShouldGroupByWeekday_Monday()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            // Monday observations (2026-01-12)
            CreateObservation(flight.Id, new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 12, 14, 0, 0, DateTimeKind.Utc), 150m),
        };

        var expectedMonday = new WeekdayStatsBucketDto
        {
            Weekday = 1,
            Label = "Mon",
            Count = 2,
            Min = 100m,
            Max = 150m,
            Avg = 125m,
            Median = 125m
        };

        // Act
        var result = _service.ComputeWeekdayStats(flight, observations);

        // Assert
        var monday = result.Series.First(s => s.Weekday == 1);
        Assert.Equivalent(expectedMonday, monday);
    }

    [Fact]
    public void ComputeWeekdayStats_ShouldGroupByWeekday_Tuesday()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            // Tuesday observation (2026-01-13)
            CreateObservation(flight.Id, new DateTime(2026, 1, 13, 10, 0, 0, DateTimeKind.Utc), 200m),
        };

        var expectedTuesday = new WeekdayStatsBucketDto
        {
            Weekday = 2,
            Label = "Tue",
            Count = 1,
            Min = 200m,
            Max = 200m,
            Avg = 200m,
            Median = 200m
        };

        // Act
        var result = _service.ComputeWeekdayStats(flight, observations);

        // Assert
        var tuesday = result.Series.First(s => s.Weekday == 2);
        Assert.Equivalent(expectedTuesday, tuesday);
    }

    [Fact]
    public void ComputeWeekdayStats_ShouldReturnAllSevenWeekdays()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, new DateTime(2026, 1, 12, 10, 0, 0, DateTimeKind.Utc), 100m),
        };

        // Act
        var result = _service.ComputeWeekdayStats(flight, observations);

        // Assert
        Assert.Equal(7, result.Series.Count);
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
    }

    [Fact]
    public void ComputeWeekdayStats_EmptyObservations_ShouldHaveNullStats()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>();

        // Act
        var result = _service.ComputeWeekdayStats(flight, observations);

        // Assert
        Assert.All(result.Series, bucket => Assert.Null(bucket.Min));
    }

    #endregion

    #region ComputeBookingDateStats Tests

    [Fact]
    public void ComputeBookingDateStats_ShouldGroupByDate_FirstDay()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 14, 0, 0, DateTimeKind.Utc), 120m),
        };

        var expectedFirstDay = new BookingDateStatsBucketDto
        {
            Date = "2026-01-10",
            Count = 2,
            Min = 100m,
            Max = 120m,
            Avg = 110m,
            Median = 110m
        };

        // Act
        var result = _service.ComputeBookingDateStats(flight, observations);

        // Assert
        Assert.Equivalent(expectedFirstDay, result.Series[0]);
    }

    [Fact]
    public void ComputeBookingDateStats_ShouldGroupByDate_SecondDay()
    {
        // Arrange
        var flight = CreateTestFlight();
        var observations = new List<ObservationEntity>
        {
            CreateObservation(flight.Id, new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc), 150m),
        };

        var expectedSecondDay = new BookingDateStatsBucketDto
        {
            Date = "2026-01-11",
            Count = 1,
            Min = 150m,
            Max = 150m,
            Avg = 150m,
            Median = 150m
        };

        // Act
        var result = _service.ComputeBookingDateStats(flight, observations);

        // Assert
        Assert.Equivalent(expectedSecondDay, result.Series[0]);
    }

    [Fact]
    public void ComputeBookingDateStats_ShouldOrderByDate_Count()
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
    }

    [Fact]
    public void ComputeBookingDateStats_ShouldOrderByDate_FirstDate()
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
        Assert.Equal("2026-01-10", result.Series[0].Date);
    }

    [Fact]
    public void ComputeBookingDateStats_ShouldOrderByDate_SecondDate()
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
        Assert.Equal("2026-01-12", result.Series[1].Date);
    }

    [Fact]
    public void ComputeBookingDateStats_ShouldOrderByDate_ThirdDate()
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
        Assert.Equal("2026-01-15", result.Series[2].Date);
    }

    #endregion

    #region ComputeDaysToDepartureStats Tests

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket1_ShouldGroupByDay_Count()
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
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket1_ShouldGroupByDay_FirstBucket()
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
        Assert.Equal(5, result.Series[0].DaysFrom);
        Assert.Equal(6, result.Series[0].DaysTo);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket1_ShouldGroupByDay_SecondBucket()
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
        Assert.Equal(9, result.Series[1].DaysFrom);
        Assert.Equal(10, result.Series[1].DaysTo);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket1_ShouldGroupByDay_ThirdBucket()
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
        Assert.Equal(10, result.Series[2].DaysFrom);
        Assert.Equal(11, result.Series[2].DaysTo);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket7_FirstBucket()
    {
        // Arrange
        var flight = CreateTestFlight();
        flight.DepartureDate = new DateTime(2026, 1, 30);
        var observations = new List<ObservationEntity>
        {
            // 5 days before (bucket 0-7)
            CreateObservation(flight.Id, new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc), 200m),
        };

        var expectedBucket = new DaysToDepartureStatsBucketDto
        {
            DaysFrom = 0,
            DaysTo = 7,
            Count = 1,
            Min = 200m,
            Max = 200m,
            Avg = 200m,
            Median = 200m
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 7);

        // Assert
        Assert.Equivalent(expectedBucket, result.Series[0]);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket7_SecondBucket()
    {
        // Arrange
        var flight = CreateTestFlight();
        flight.DepartureDate = new DateTime(2026, 1, 30);
        var observations = new List<ObservationEntity>
        {
            // 10 days before (bucket 7-14)
            CreateObservation(flight.Id, new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc), 150m),
        };

        var expectedBucket = new DaysToDepartureStatsBucketDto
        {
            DaysFrom = 7,
            DaysTo = 14,
            Count = 1,
            Min = 150m,
            Max = 150m,
            Avg = 150m,
            Median = 150m
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 7);

        // Assert
        Assert.Equivalent(expectedBucket, result.Series[0]);
    }

    [Fact]
    public void ComputeDaysToDepartureStats_Bucket7_ThirdBucket()
    {
        // Arrange
        var flight = CreateTestFlight();
        flight.DepartureDate = new DateTime(2026, 1, 30);
        var observations = new List<ObservationEntity>
        {
            // 20 days before (bucket 14-21)
            CreateObservation(flight.Id, new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc), 100m),
            CreateObservation(flight.Id, new DateTime(2026, 1, 11, 12, 0, 0, DateTimeKind.Utc), 110m),
        };

        var expectedBucket = new DaysToDepartureStatsBucketDto
        {
            DaysFrom = 14,
            DaysTo = 21,
            Count = 2,
            Min = 100m,
            Max = 110m,
            Avg = 105m,
            Median = 105m
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 7);

        // Assert
        Assert.Equivalent(expectedBucket, result.Series[0]);
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

        var expectedBucket = new DaysToDepartureStatsBucketDto
        {
            DaysFrom = 5,
            DaysTo = 6,
            Count = 1,
            Min = 100m,
            Max = 100m,
            Avg = 100m,
            Median = 100m
        };

        // Act
        var result = _service.ComputeDaysToDepartureStats(flight, observations, bucket: 1);

        // Assert
        Assert.Single(result.Series);
        Assert.Equivalent(expectedBucket, result.Series[0]);
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
