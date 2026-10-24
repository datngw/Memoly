using Memoly.Api.Features.System;
using Memoly.Api.Shared;
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

    builder.Host.UseSerilog((ctx, _, lc) =>
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console();
        if (ctx.Configuration.GetConnectionString("seq") is { } seqUrl)
        {
            lc.WriteTo.Seq(seqUrl);
        }
    });

    builder.Host.UseWolverine(opts =>
    {
        // Handlers live in feature slices; discovered from this assembly only.
        opts.Discovery.IncludeAssembly(typeof(Program).Assembly);

        // EF Core durable messaging: envelope storage commits with the data.
        opts.Services.AddDbContextWithWolverineIntegration<MemolyDbContext>(o =>
            o.UseNpgsql(builder.Configuration.GetConnectionString("memoly")));

        opts.UseEntityFrameworkCoreTransactions();
        opts.Policies.AutoApplyTransactions();
    });

    builder.Services.AddMemoly(builder.Configuration);
    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
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
