using System;
using FlightTracker.Api.Storage.Entities;
using LiteDB;

namespace FlightTracker.Api.Infrastructure.LiteDb;

public class LiteDbContext : IDisposable
{
    private readonly LiteDatabase _database;

    public LiteDbContext(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("connectionString is required", nameof(connectionString));

        _database = new LiteDatabase(connectionString);

        // initialize collection references
        Flights = _database.GetCollection<FlightEntity>("flights");
        Observations = _database.GetCollection<ObservationEntity>("observations");
        Queries = _database.GetCollection<QueryEntity>("queries");

        // Configure indices for Flights collection
        // Unique index on (FlightNumber, DepartureDate, OriginIata, DestinationIata)
        Flights.EnsureIndex(x => x.FlightNumber);
        Flights.EnsureIndex(x => x.DepartureDate);
        Flights.EnsureIndex(x => x.OriginIata);
        Flights.EnsureIndex(x => x.DestinationIata);

        // Configure indices for Observations collection
        Observations.EnsureIndex(x => x.FlightId);
        Observations.EnsureIndex(x => x.ObservedAtUtc);
    }

    public ILiteCollection<FlightEntity> Flights { get; }
    public ILiteCollection<ObservationEntity> Observations { get; }

    public ILiteCollection<QueryEntity> Queries { get; }

    public void Dispose()
    {
        _database?.Dispose();
    }
}
