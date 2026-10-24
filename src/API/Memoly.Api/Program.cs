using Memoly.Api.Shared.Database;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

// Two-stage Serilog bootstrap: a console logger for startup failures, the real
// logger from configuration once the host is built (Shared/Database migrations
// and Wolverine wiring come later; this file stays the thin composition root).
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

    builder.Services.AddOpenApi();

    builder.Services.AddDbContext<MemolyDbContext>(o =>
        o.UseNpgsql(builder.Configuration.GetConnectionString("memoly")));

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.MapDefaultEndpoints();
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();

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
