using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;
using LiteDB;

namespace FlightTracker.Api.Storage.Repositories;

public class ScheduleRepository : IScheduleRepository
{
    private readonly ILiteCollection<ScrapeSchedule> _collection;

    public ScheduleRepository(LiteDbContext context)
    {
        _collection = context.Database.GetCollection<ScrapeSchedule>("schedules");
    }

    public IEnumerable<ScrapeSchedule> GetAll()
    {
        return _collection.FindAll().ToList();
    }

    public void SetSchedules(IEnumerable<TimeOnly> times)
    {
        _collection.DeleteAll();
        
        var schedules = times.Select((time, index) => new ScrapeSchedule
        {
            Id = index + 1,
            Time = time
        });
        
        _collection.InsertBulk(schedules);
    }
}
