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
        private readonly IFlightScraper _provider;
        private readonly FlightSnapshotRepository _repository;

        public FlightCollectionService(IFlightScraper provider, FlightSnapshotRepository repository)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<FlightDto>> CollectAndStoreAsync(FlightQueryDto query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            throw new NotImplementedException();
        }
    }
}
