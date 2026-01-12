using System;
using System.Collections.Generic;
using FlightTracker.Api.Storage.Entities;
using FlightTracker.Api.Storage.Repositories;
using LiteDB;

namespace FlightTracker.Api.Services
{
    public class DataSeederService
    {
        private readonly IFlightRepository _flightRepository;
        private readonly IObservationRepository _observationRepository;

        public DataSeederService(
            IFlightRepository flightRepository,
            IObservationRepository observationRepository)
        {
            _flightRepository = flightRepository;
            _observationRepository = observationRepository;
        }

        public void SeedSampleData()
        {
            // Create sample flights
            var flight1 = CreateOrGetFlight("LX1070", new DateTime(2026, 2, 15), "ZRH", "BCN");
            var flight2 = CreateOrGetFlight("LX8080", new DateTime(2026, 2, 20), "ZRH", "JFK");
            var flight3 = CreateOrGetFlight("LX1071", new DateTime(2026, 2, 15), "ZRH", "BCN");

            // Seed observations for flight1
            SeedObservationsForFlight(flight1.Id, new DateTime(2026, 2, 15), new[]
            {
                (new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc), 150m),
                (new DateTime(2026, 1, 10, 14, 0, 0, DateTimeKind.Utc), 155m),
                (new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc), 145m),
                (new DateTime(2026, 1, 12, 11, 0, 0, DateTimeKind.Utc), 160m),
                (new DateTime(2026, 1, 13, 9, 0, 0, DateTimeKind.Utc), 148m),
                (new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), 152m),
                (new DateTime(2026, 1, 20, 14, 0, 0, DateTimeKind.Utc), 170m),
                (new DateTime(2026, 1, 25, 9, 0, 0, DateTimeKind.Utc), 180m),
                (new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc), 200m),
                (new DateTime(2026, 2, 5, 11, 0, 0, DateTimeKind.Utc), 220m),
                (new DateTime(2026, 2, 10, 9, 0, 0, DateTimeKind.Utc), 250m),
            });

            // Seed observations for flight2
            SeedObservationsForFlight(flight2.Id, new DateTime(2026, 2, 20), new[]
            {
                (new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), 450m),
                (new DateTime(2026, 1, 20, 11, 0, 0, DateTimeKind.Utc), 480m),
                (new DateTime(2026, 1, 25, 9, 0, 0, DateTimeKind.Utc), 500m),
                (new DateTime(2026, 2, 1, 14, 0, 0, DateTimeKind.Utc), 520m),
                (new DateTime(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc), 580m),
            });

            // Seed observations for flight3 (same route, different flight)
            SeedObservationsForFlight(flight3.Id, new DateTime(2026, 2, 15), new[]
            {
                (new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc), 140m),
                (new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc), 135m),
                (new DateTime(2026, 1, 15, 14, 0, 0, DateTimeKind.Utc), 138m),
                (new DateTime(2026, 1, 20, 11, 0, 0, DateTimeKind.Utc), 142m),
            });
        }

        private FlightEntity CreateOrGetFlight(string flightNumber, DateTime departureDate, string origin, string destination)
        {
            var existing = _flightRepository.FindUnique(flightNumber, departureDate, origin, destination);
            if (existing != null)
            {
                return existing;
            }

            var flight = new FlightEntity
            {
                FlightNumber = flightNumber,
                DepartureDate = departureDate,
                OriginIata = origin,
                DestinationIata = destination
            };

            _flightRepository.Insert(flight);
            return flight;
        }

        private void SeedObservationsForFlight(ObjectId flightId, DateTime departureDate, (DateTime observedAt, decimal price)[] observations)
        {
            var entities = new List<ObservationEntity>();

            foreach (var (observedAt, price) in observations)
            {
                entities.Add(new ObservationEntity
                {
                    FlightId = flightId,
                    ObservedAtUtc = observedAt,
                    PriceChf = price
                });
            }

            _observationRepository.InsertMany(entities);
        }
    }
}
