using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories.Abstract;
using FlightTracker.Contracts;

namespace FlightTracker.Api.Storage.Repositories;

public class FlightQueryRepository : IFlightQueryRepository
{
    private readonly LiteDbContext _context;

    public FlightQueryRepository(LiteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Insert(FlightQueryDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var entity = new FlightQueryEntity
        {
            Origin = dto.Origin,
            Destination = dto.Destination,
            FlexibilityDays = dto.FlexibilityDays,
            DepartureDate = dto.DepartureDate
        };
        _context.FlightQueries.Insert(entity);
    }

    public List<FlightQueryEntity> GetAll()
    {
        return _context.FlightQueries.FindAll().ToList();
    }
}
