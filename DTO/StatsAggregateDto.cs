namespace FlightTracker.Contracts;

public class StatsAggregateDto
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public decimal? Avg { get; set; }
    public decimal? Median { get; set; }
    public int Count { get; set; }
}
