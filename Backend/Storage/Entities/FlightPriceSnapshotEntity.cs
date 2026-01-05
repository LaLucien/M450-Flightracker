using System;
using LiteDB;

namespace FlightTracker.Api.Storage.Entities
{
    public class FlightPriceSnapshotEntity
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Airline { get; set; } = string.Empty;
        public DateTime DepartureDate { get; set; }
        public decimal Price { get; set; }
        public DateTime CheckedAt { get; set; }
    }
}
