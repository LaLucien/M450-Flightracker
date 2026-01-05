using LiteDB;

namespace FlightTracker.Api.Storage.Entities;

public class FlightQueryEntity
{
    
    public int Id { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public int FlexibilityDays { get; set; } = 0;
    public DateTime DepartureDate { get; set; }
}
