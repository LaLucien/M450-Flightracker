namespace FlightTracker.Contracts;

public class DaysToDepartureStatsBucketDto : StatsAggregateDto
{
    /// <summary>
    /// Starting number of days to departure (inclusive).
    /// Together with DaysTo defines the range [DaysFrom, DaysTo) for this bucket.
    /// </summary>
    public int DaysFrom { get; set; }
    /// <summary>
    /// Ending number of days to departure (exclusive).
    /// Together with DaysFrom defines the range [DaysFrom, DaysTo) for this bucket.
    /// </summary>
    public int DaysTo { get; set; }
}

public class DaysToDepartureStatsResponseDto
{
    public string FlightId { get; set; } = string.Empty;
    public FlightResponseDto Flight { get; set; } = new();
    public string Timezone { get; set; } = "Europe/Zurich";
    public List<DaysToDepartureStatsBucketDto> Series { get; set; } = new();
}
