namespace FlightTracker.Contracts;

public class FlexStatsBucketDto
{
    public string DepartureDate { get; set; } = string.Empty; // YYYY-MM-DD
    public decimal MedianPriceChf { get; set; }
    public string FlightId { get; set; } = string.Empty;
}

public class FlexStatsResponseDto
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string TargetDate { get; set; } = string.Empty;
    public int FlexDays { get; set; }
    public string Timezone { get; set; } = "Europe/Zurich";
    public List<FlexStatsBucketDto> Series { get; set; } = new();
    public FlexStatsBucketDto? Best { get; set; }
}
