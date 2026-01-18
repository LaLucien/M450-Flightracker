namespace FlightTracker.Contracts;

public class DaysToDepartureStatsBucketDto
{
 public int DaysFrom { get; set; }
 public int DaysTo { get; set; }
 public decimal? Min { get; set; }
 public decimal? Max { get; set; }
 public decimal? Avg { get; set; }
 public decimal? Median { get; set; }
 public int Count { get; set; }
}

public class DaysToDepartureStatsResponseDto
{
 public string FlightId { get; set; } = string.Empty;
 public FlightResponseDto Flight { get; set; } = new FlightResponseDto();
 public string Timezone { get; set; } = string.Empty;
 public List<DaysToDepartureStatsBucketDto> Series { get; set; } = new();
}
