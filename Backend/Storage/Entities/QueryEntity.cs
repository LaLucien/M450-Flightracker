using LiteDB;

namespace FlightTracker.Api.Storage.Entities;

public class QueryEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    public required string OriginIata { get; set; }

    public required string DestinationIata { get; set; }

    public int FlexibilityDays { get; set; } = 0;

    public DateTime AnchorDate { get; set; }
}
