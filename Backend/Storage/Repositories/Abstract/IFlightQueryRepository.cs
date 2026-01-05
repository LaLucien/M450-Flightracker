using FlightTracker.Api.Storage.Entities;
using FlightTracker.Contracts;

namespace FlightTracker.Api.Storage.Repositories.Abstract
{
    public interface IFlightQueryRepository
    {
        List<FlightQueryEntity> GetAll();
        void Insert(FlightQueryDto dto);
    }
}