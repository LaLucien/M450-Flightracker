using System;
using System.Collections.Generic;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Contracts;

namespace FlightTracker.Api.Services;

public interface IFlightStatsService
{
    DateTime ConvertUtcToZurich(DateTime utc);
    DateOnly GetBookingDateLocal(DateTime utc);
    int GetWeekdayLocal(DateTime utc);
    string GetWeekdayLabel(int weekday);
    int GetDaysToDeparture(DateTime observedAtUtc, DateTime departureDate);
    decimal? Median(IEnumerable<decimal> values);
    StatsAggregateDto AggregateStats(List<decimal> prices);
    FlightResponseDto ToFlightResponseDto(FlightEntity entity);
    WeekdayStatsResponseDto ComputeWeekdayStats(FlightEntity flight, List<ObservationEntity> observations);
    BookingDateStatsResponseDto ComputeBookingDateStats(FlightEntity flight, List<ObservationEntity> observations);
    DaysToDepartureStatsResponseDto ComputeDaysToDepartureStats(FlightEntity flight, List<ObservationEntity> observations, int bucket);
}
