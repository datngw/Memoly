using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Memoly.Api.Shared.Database;

public sealed class MemolyDbContext(DbContextOptions<MemolyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Wolverine inbox/outbox envelope tables: durable messages commit with the data
        // in the same transaction (cross-feature side effects ride these tables).
        modelBuilder.MapWolverineEnvelopeStorage();
    }
}
