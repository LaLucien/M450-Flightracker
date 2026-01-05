using FlightTracker.Contracts;
using Microsoft.AspNetCore.Mvc;
using FlightTracker.Api.Services.Selenium;
using FlightTracker.Api.Storage.Repositories;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Controllers
{
    [ApiController]
    [Route("api/flights")]
    public class FlightsController : ControllerBase
    {
        private readonly FlightCollectionService _collectionService;
        private readonly FlightSnapshotRepository _repository;

        public FlightsController(FlightCollectionService collectionService, FlightSnapshotRepository repository)
        {
            _collectionService = collectionService;
            _repository = repository;
        }

        [HttpGet]
        public IEnumerable<FlightDto> Get()
        {
            return new[]
            {
            new FlightDto
            {
                Id = 1,
                Origin = "Zürich",
                Destination = "London",
                Airline = "Swiss",
                DepartureDate = DateTime.Today.AddDays(10),
                Price = 120
            }
        };
        }

        [HttpPost("collect")]
        public async Task<ActionResult<List<FlightDto>>> Collect([FromBody] FlightQueryDto query)
        {
            if (query == null) return BadRequest();

            var flights = await _collectionService.CollectAndStoreAsync(query).ConfigureAwait(false);
            return Ok(flights);
        }

        [HttpGet("latest")]
        public ActionResult<List<FlightPriceSnapshotEntity>> GetLatest([FromQuery] int count = 50)
        {
            var snapshots = _repository.GetLatest(count);
            return Ok(snapshots);
        }
    }
}
