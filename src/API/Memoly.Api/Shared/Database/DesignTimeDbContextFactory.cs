using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;

namespace Memoly.Api.Shared.Database;

/// <summary>
/// Lets `dotnet ef` build the model without running the host (no Aspire, no containers).
/// Registers through Wolverine's integration so the envelope storage entities are part
/// of the model — a plain options builder would skip them and produce empty migrations.
/// Migrations are only generated against this placeholder connection, never applied.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MemolyDbContext>
{
    public MemolyDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.AddDbContextWithWolverineIntegration<MemolyDbContext>(o =>
            o.UseNpgsql("Host=localhost;Database=memoly;Username=postgres;Password=postgres"));

        var options = services.BuildServiceProvider()
            .GetRequiredService<DbContextOptions<MemolyDbContext>>();

        return new MemolyDbContext(options);
    }
}
