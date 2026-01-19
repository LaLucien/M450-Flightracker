using System;

namespace FlightTracker.Contracts;

public class QueryResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string OriginIata { get; set; } = string.Empty;
    public string DestinationIata { get; set; } = string.Empty;
    public DateTime DepartureDate { get; set; }
    public int FlexibilityDays { get; set; }
}
