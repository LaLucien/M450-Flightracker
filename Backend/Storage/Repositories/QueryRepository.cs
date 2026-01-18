using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using LiteDB;

namespace FlightTracker.Api.Storage.Repositories;

public class QueryRepository(LiteDbContext context) : IQueryRepository
{
    private readonly LiteDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public void Insert(QueryEntity entity)
    {
        _context.Queries.Insert(entity);
    }

    public List<QueryEntity> GetAll()
    {
        return _context.Queries.Query().ToList();
    }

    public bool Delete(string id)
    {
        try
        {
            var objectId = new ObjectId(id);
            return _context.Queries.Delete(objectId);
        }
        catch
        {
            return false;
        }
    }
}
