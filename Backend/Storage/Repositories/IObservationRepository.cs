using System;
using System.Collections.Generic;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Storage.Repositories;

public interface IObservationRepository
{
    List<ObservationEntity> GetByFlightId(string flightId);
    List<ObservationEntity> GetByFlightIdWithDateFilter(string flightId, DateTime? fromUtc = null, DateTime? toUtc = null);
    void Insert(ObservationEntity entity);
    void InsertMany(IEnumerable<ObservationEntity> entities);
}
