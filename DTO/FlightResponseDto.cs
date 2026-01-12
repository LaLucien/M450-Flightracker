namespace FlightTracker.Contracts;

public class FlightResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public string DepartureDate { get; set; } = string.Empty; // YYYY-MM-DD
    public string OriginIata { get; set; } = string.Empty;
    public string DestinationIata { get; set; } = string.Empty;
}
