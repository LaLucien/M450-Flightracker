namespace FlightTracker.Contracts;

public class BookingDateStatsBucketDto : StatsAggregateDto
{
    public string Date { get; set; } = string.Empty; // YYYY-MM-DD
}

public class BookingDateStatsResponseDto
{
    public string FlightId { get; set; } = string.Empty;
    public FlightResponseDto Flight { get; set; } = new();
    public string Timezone { get; set; } = "Europe/Zurich";
    public List<BookingDateStatsBucketDto> Series { get; set; } = new();
}
