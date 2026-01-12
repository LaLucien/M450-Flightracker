using System;
using LiteDB;

namespace FlightTracker.Api.Storage.Entities;

public class ObservationEntity
{
    [BsonId]
    public ObjectId Id { get; set; }

    public ObjectId FlightId { get; set; }

    public DateTime ObservedAtUtc { get; set; }

    public decimal PriceChf { get; set; }
}
