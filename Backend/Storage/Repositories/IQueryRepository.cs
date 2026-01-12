using System.Collections.Generic;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Storage.Repositories;

public interface IQueryRepository
{
    List<QueryEntity> GetAll();
    void Insert(QueryEntity entity);
}
