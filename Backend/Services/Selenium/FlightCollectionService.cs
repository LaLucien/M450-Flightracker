using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightTracker.Contracts;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Services.Selenium
{
    public class FlightCollectionService
    {
        private readonly IFlightProvider _provider;
        private readonly FlightSnapshotRepository _repository;

        public FlightCollectionService(IFlightProvider provider, FlightSnapshotRepository repository)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<FlightDto>> CollectAndStoreAsync(FlightQueryDto query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var flights = await _provider.FetchFlightsAsync(query).ConfigureAwait(false);
            if (flights == null || !flights.Any()) return flights ?? new List<FlightDto>();

            var now = DateTime.UtcNow;

            var entities = flights.Select(f => new FlightPriceSnapshotEntity
            {
                Origin = f.Origin,
                Destination = f.Destination,
                Airline = f.Airline,
                DepartureDate = f.DepartureDate,
                Price = f.Price,
                CheckedAt = now
            }).ToList();

            _repository.InsertMany(entities);

            return flights;
        }
    }
}
