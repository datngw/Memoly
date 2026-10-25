using Memoly.Api.Features.System;
using Memoly.Api.Shared;
using Memoly.Api.Shared.Auth;
using Memoly.Api.Shared.Database;
using Memoly.Api.Shared.Presentation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.EntityFrameworkCore;

// Two-stage Serilog bootstrap: a console logger for startup failures, the real
// logger from configuration once the host is built. This file stays the thin
// composition root: services, Wolverine, endpoint mapping.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // preserveStaticLogger: each host (app or WebApplicationFactory in tests) builds its
    // own logger; the shared static reloadable logger cannot be frozen twice.
    builder.Host.UseSerilog(
        (ctx, _, lc) =>
        {
            lc.ReadFrom.Configuration(ctx.Configuration)
                .WriteTo.Console();
            if (ctx.Configuration.GetConnectionString("seq") is { } seqUrl)
            {
                lc.WriteTo.Seq(seqUrl);
            }
        },
        preserveStaticLogger: true);

    builder.Host.UseWolverine(opts =>
    {
        // Handlers live in feature slices; discovered from this assembly only.
        opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

        // EF Core durable messaging: envelope storage commits with the data.
        opts.Services.AddDbContextWithWolverineIntegration<MemolyDbContext>(o =>
            o.UseNpgsql(builder.Configuration.GetConnectionString("memoly")));

        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.AutoApplyTransactions();

        // Provisions the Wolverine envelope tables (and application tables) at startup
        // against the live database — dev-time convenience owned by Weasel.
        opts.UseEntityFrameworkCoreWolverineManagedMigrations();
    });

    builder.Services.AddMemoly(builder.Configuration);
    builder.Services.AddMemolyAuthentication(builder.Configuration, builder.Environment);
    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapDefaultEndpoints();
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
    app.MapSystemEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
