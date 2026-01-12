using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using System.Linq.Expressions;

namespace FlightTracker.Api.Storage.Repositories;

public class ObservationRepository
{
    private readonly LiteDbContext _context;

    public ObservationRepository(LiteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public List<ObservationEntity> GetByFlightId(string flightId)
    {
        ObjectId objectId;
        try
        {
            objectId = new ObjectId(flightId);
        }
        catch
        {
            return new List<ObservationEntity>();
        }
        return _context.Observations
            .Query()
            .Where(x => x.FlightId == objectId)
            .OrderBy(x => x.ObservedAtUtc)
            .ToList();
    }

    public List<ObservationEntity> GetByFlightIdWithDateFilter(string flightId, DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        ObjectId objectId;
        try
        {
            objectId = new ObjectId(flightId);
        }
        catch
        {
            return new List<ObservationEntity>();
        }
        var query = _context.Observations
            .Query()
            .Where(x => x.FlightId == objectId);

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.ObservedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.ObservedAtUtc < toUtc.Value);
        }

        return query.OrderBy(x => x.ObservedAtUtc).ToList();
    }

    public void Insert(ObservationEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        _context.Observations.Insert(entity);
    }

    public void InsertMany(IEnumerable<ObservationEntity> entities)
    {
        if (entities == null) return;
        _context.Observations.InsertBulk(entities);
    }
}
