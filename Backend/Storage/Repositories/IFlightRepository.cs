using System;
using System.Collections.Generic;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Storage.Repositories;

public interface IFlightRepository
{
    FlightEntity? GetById(string id);
    List<FlightEntity> Query(string? origin = null, string? destination = null, DateTime? departureDate = null, string? flightNumber = null);
    FlightEntity? FindUnique(string flightNumber, DateTime departureDate, string originIata, string destinationIata);
    void Insert(FlightEntity entity);
    void Update(FlightEntity entity);
}
