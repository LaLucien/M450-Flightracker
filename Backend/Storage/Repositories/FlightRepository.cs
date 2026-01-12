using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Storage.Repositories;

public class FlightRepository
{
    private readonly LiteDbContext _context;

    public FlightRepository(LiteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public FlightEntity? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        // We need this try-catch because ObjectId constructor throws if the format is invalid
        try
        {
            var objectId = new ObjectId(id);
            return _context.Flights.FindById(objectId);
        }
        catch
        {
            return null;
        }
    }

    public List<FlightEntity> Query(string? origin = null, string? destination = null, DateTime? departureDate = null, string? flightNumber = null)
    {
        var date = departureDate?.Date;

        return _context.Flights.Query()
            .Where(x =>
                (string.IsNullOrWhiteSpace(origin) || x.OriginIata == origin) &&
                (string.IsNullOrWhiteSpace(destination) || x.DestinationIata == destination) &&
                (!date.HasValue || (x.DepartureDate >= date && x.DepartureDate < date.Value.AddDays(1))) &&
                (string.IsNullOrWhiteSpace(flightNumber) || x.FlightNumber == flightNumber)
            )
            .ToList();
    }

    public FlightEntity? FindUnique(string flightNumber, DateTime departureDate, string originIata, string destinationIata)
    {
        var date = departureDate.Date;
        return _context.Flights
            .Query()
            .Where(x => x.FlightNumber == flightNumber
                && x.DepartureDate >= date && x.DepartureDate < date.AddDays(1)
                && x.OriginIata == originIata
                && x.DestinationIata == destinationIata)
            .FirstOrDefault();
    }

    public void Insert(FlightEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        _context.Flights.Insert(entity);
    }

    public void Update(FlightEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        _context.Flights.Update(entity);
    }
}
