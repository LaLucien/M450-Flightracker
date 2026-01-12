namespace FlightTracker.Contracts;

public class WeekdayStatsBucketDto : StatsAggregateDto
{
    public int Weekday { get; set; } // 1..7 (Mon..Sun)
    public string Label { get; set; } = string.Empty; // "Mon".."Sun"
}

public class WeekdayStatsResponseDto
{
    public string FlightId { get; set; } = string.Empty;
    public FlightResponseDto Flight { get; set; } = new();
    public string Timezone { get; set; } = "Europe/Zurich";
    public List<WeekdayStatsBucketDto> Series { get; set; } = new();
}
