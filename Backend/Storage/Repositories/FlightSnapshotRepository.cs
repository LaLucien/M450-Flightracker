using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using FlightTracker.Api.Infrastructure.LiteDb;
using FlightTracker.Api.Storage.Entities;

namespace FlightTracker.Api.Storage.Repositories
{
    public class FlightSnapshotRepository
    {
        private readonly LiteDbContext _context;

        public FlightSnapshotRepository(LiteDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // Ensure index on CheckedAt for efficient ordering queries
            _context.FlightSnapshots.EnsureIndex(x => x.CheckedAt);
        }

        public void InsertMany(IEnumerable<FlightPriceSnapshotEntity> entities)
        {
            if (entities == null) return;

            // Use InsertBulk for better performance when inserting many documents
            _context.FlightSnapshots.InsertBulk(entities);
        }

        public List<FlightPriceSnapshotEntity> GetLatest(int count = 50)
        {
            if (count <= 0) return new List<FlightPriceSnapshotEntity>();

            // Query and order by CheckedAt descending, then limit to requested count
            var results = _context.FlightSnapshots
                .Query()
                .OrderByDescending("CheckedAt")
                .Limit(count)
                .ToList();

            return results;
        }
    }
}
