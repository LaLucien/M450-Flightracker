namespace FlightTracker.Contracts;

public class BookingDateStatsBucketDto
{
 public string Date { get; set; } = string.Empty;
 public decimal? Min { get; set; }
 public decimal? Max { get; set; }
 public decimal? Avg { get; set; }
 public decimal? Median { get; set; }
 public int Count { get; set; }
}

public class BookingDateStatsResponseDto
{
 public string FlightId { get; set; } = string.Empty;
 public FlightResponseDto Flight { get; set; } = new FlightResponseDto();
 public string Timezone { get; set; } = string.Empty;
 public List<BookingDateStatsBucketDto> Series { get; set; } = new();
}
