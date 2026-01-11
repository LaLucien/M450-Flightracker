using System;
using System.Collections.Generic;
using System.Linq;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Contracts;

namespace FlightTracker.Api.Services;

public class FlightStatsService
{
    private static readonly TimeZoneInfo ZurichTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

    public DateTime ConvertUtcToZurich(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utc, ZurichTimeZone);
    }

    public DateOnly GetBookingDateLocal(DateTime utc)
    {
        var localTime = ConvertUtcToZurich(utc);
        return DateOnly.FromDateTime(localTime);
    }

    public int GetWeekdayLocal(DateTime utc)
    {
        var localTime = ConvertUtcToZurich(utc);
        // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
        // Convert to Monday=1, ..., Sunday=7
        var dayOfWeek = (int)localTime.DayOfWeek;
        return dayOfWeek == 0 ? 7 : dayOfWeek;
    }

    public string GetWeekdayLabel(int weekday)
    {
        return weekday switch
        {
            1 => "Mon",
            2 => "Tue",
            3 => "Wed",
            4 => "Thu",
            5 => "Fri",
            6 => "Sat",
            7 => "Sun",
            _ => string.Empty
        };
    }

    public int GetDaysToDeparture(DateTime observedAtUtc, DateTime departureDate)
    {
        var bookingDateLocal = GetBookingDateLocal(observedAtUtc);
        var departureDateOnly = DateOnly.FromDateTime(departureDate.Date);
        return departureDateOnly.DayNumber - bookingDateLocal.DayNumber;
    }

    public decimal? Median(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return null;

        var sorted = list.OrderBy(x => x).ToList();
        var count = sorted.Count;

        if (count % 2 == 1)
        {
            // Odd: return middle element
            return sorted[count / 2];
        }
        else
        {
            // Even: return average of two middle elements
            var mid1 = sorted[count / 2 - 1];
            var mid2 = sorted[count / 2];
            return (mid1 + mid2) / 2m;
        }
    }

    public StatsAggregateDto AggregateStats(List<decimal> prices)
    {
        if (prices.Count == 0)
        {
            return new StatsAggregateDto
            {
                Min = null,
                Max = null,
                Avg = null,
                Median = null,
                Count = 0
            };
        }

        return new StatsAggregateDto
        {
            Min = prices.Min(),
            Max = prices.Max(),
            Avg = prices.Average(),
            Median = Median(prices),
            Count = prices.Count
        };
    }

    public FlightResponseDto ToFlightResponseDto(FlightEntity entity)
    {
        return new FlightResponseDto
        {
            Id = entity.Id.ToString(),
            FlightNumber = entity.FlightNumber,
            DepartureDate = DateOnly.FromDateTime(entity.DepartureDate.Date).ToString("yyyy-MM-dd"),
            OriginIata = entity.OriginIata,
            DestinationIata = entity.DestinationIata
        };
    }

    public WeekdayStatsResponseDto ComputeWeekdayStats(FlightEntity flight, List<ObservationEntity> observations)
    {
        var weekdayGroups = observations
            .GroupBy(obs => GetWeekdayLocal(obs.ObservedAtUtc))
            .ToDictionary(g => g.Key, g => g.Select(o => o.PriceChf).ToList());

        var series = new List<WeekdayStatsBucketDto>();
        for (int weekday = 1; weekday <= 7; weekday++)
        {
            var prices = weekdayGroups.ContainsKey(weekday) ? weekdayGroups[weekday] : new List<decimal>();
            var stats = AggregateStats(prices);

            series.Add(new WeekdayStatsBucketDto
            {
                Weekday = weekday,
                Label = GetWeekdayLabel(weekday),
                Min = stats.Min,
                Max = stats.Max,
                Avg = stats.Avg,
                Median = stats.Median,
                Count = stats.Count
            });
        }

        return new WeekdayStatsResponseDto
        {
            FlightId = flight.Id.ToString(),
            Flight = ToFlightResponseDto(flight),
            Timezone = "Europe/Zurich",
            Series = series
        };
    }

    public BookingDateStatsResponseDto ComputeBookingDateStats(FlightEntity flight, List<ObservationEntity> observations)
    {
        var dateGroups = observations
            .GroupBy(obs => GetBookingDateLocal(obs.ObservedAtUtc))
            .OrderBy(g => g.Key)
            .ToList();

        var series = dateGroups.Select(g =>
        {
            var prices = g.Select(o => o.PriceChf).ToList();
            var stats = AggregateStats(prices);

            return new BookingDateStatsBucketDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Min = stats.Min,
                Max = stats.Max,
                Avg = stats.Avg,
                Median = stats.Median,
                Count = stats.Count
            };
        }).ToList();

        return new BookingDateStatsResponseDto
        {
            FlightId = flight.Id.ToString(),
            Flight = ToFlightResponseDto(flight),
            Timezone = "Europe/Zurich",
            Series = series
        };
    }

    public DaysToDepartureStatsResponseDto ComputeDaysToDepartureStats(FlightEntity flight, List<ObservationEntity> observations, int bucket)
    {
        var daysToDepGroups = observations
            .Select(obs => new
            {
                Observation = obs,
                DaysToDeparture = GetDaysToDeparture(obs.ObservedAtUtc, flight.DepartureDate)
            })
            .Where(x => x.DaysToDeparture >= 0) // Only include non-negative days
            .GroupBy(x => (int)Math.Floor((double)x.DaysToDeparture / bucket) * bucket)
            .OrderBy(g => g.Key)
            .ToList();

        var series = daysToDepGroups.Select(g =>
        {
            var prices = g.Select(x => x.Observation.PriceChf).ToList();
            var stats = AggregateStats(prices);

            return new DaysToDepartureStatsBucketDto
            {
                DaysFrom = g.Key,
                DaysTo = g.Key + bucket,
                Min = stats.Min,
                Max = stats.Max,
                Avg = stats.Avg,
                Median = stats.Median,
                Count = stats.Count
            };
        }).ToList();

        return new DaysToDepartureStatsResponseDto
        {
            FlightId = flight.Id.ToString(),
            Flight = ToFlightResponseDto(flight),
            Timezone = "Europe/Zurich",
            Series = series
        };
    }
}
