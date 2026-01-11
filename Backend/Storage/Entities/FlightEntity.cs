using System;
using LiteDB;

namespace FlightTracker.Api.Storage.Entities;

public class FlightEntity
{
    [BsonId]
    public ObjectId Id { get; set; }

    public string FlightNumber { get; set; } = string.Empty;

    public DateTime DepartureDate { get; set; }

    public string OriginIata { get; set; } = string.Empty;

    public string DestinationIata { get; set; } = string.Empty;
}
