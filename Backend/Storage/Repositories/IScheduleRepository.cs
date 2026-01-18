using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Storage.Repositories;

public interface IScheduleRepository
{
    IEnumerable<ScrapeSchedule> GetAll();
    void SetSchedules(IEnumerable<TimeOnly> times);
}
