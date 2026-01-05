using System;
using FlightTracker.Api.Storage.Entities;
using LiteDB;

namespace FlightTracker.Api.Infrastructure.LiteDb
{
    public class LiteDbContext : IDisposable
    {
        private readonly LiteDatabase _database;
        public ILiteCollection<FlightPriceSnapshotEntity> FlightSnapshots { get; }
        public ILiteCollection<FlightQueryEntity> FlightQueries { get; }

        public LiteDbContext(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("connectionString is required", nameof(connectionString));

            _database = new LiteDatabase(connectionString);

            // initialize collection reference
            FlightSnapshots = _database.GetCollection<FlightPriceSnapshotEntity>("flight_snapshots");
            FlightQueries = _database.GetCollection<FlightQueryEntity>("flight_queries");
        }


        public void Dispose()
        {
            _database?.Dispose();
        }
    }
}
