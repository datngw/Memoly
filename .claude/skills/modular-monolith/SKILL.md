---
name: modular-monolith
description: >
  Modular Monolithic Architecture for .NET applications following the Evently pattern.
  Covers module isolation with per-module layered projects (Application, Domain,
  Infrastructure, Presentation, IntegrationEvents), schema-per-module database isolation,
  inter-module communication via integration events, a layered Common shared kernel,
  and evolution paths to microservices.
  Load this skill when the architecture-advisor recommends Modular Monolith,
  when working in a multi-module codebase, when adding a new bounded context as
  a module, or when discussing module boundaries, integration events, or
  microservices extraction.
---

# Modular Monolithic Architecture

## Core Principles

1. **Module = bounded context** — Each module represents a business capability with its own domain model, data store, and public API. Modules do not share entities or DbContexts. They communicate through well-defined contracts.
2. **Strict module isolation** — A module's internal types are `internal` by default. Only the IntegrationEvents project exposes public contracts (DTOs, integration events). No module references another module's implementation directly. The compiler enforces this via project references.
3. **Each module follows Clean Architecture internally** — Every module has 5 projects: Domain, Application, Infrastructure, Presentation, and IntegrationEvents. Domain is the center; dependencies point inward. This gives consistency across modules while keeping isolation.
4. **Database per module (schema-per-module minimum)** — Each module owns its data via its own DbContext in the Infrastructure project. Either a separate schema in the same database or a completely separate database. No module queries another module's tables directly.
5. **The API host is a thin composition root** — The API project wires modules together: registers DI, maps endpoints, and configures inter-module communication. It contains no business logic.
6. **Common is a layered shared kernel** — Split into Domain, Application, Infrastructure, and Presentation layers. Provides shared abstractions (event bus, result types, domain primitives) without coupling modules.

## Patterns

### Solution Structure

```
Evently/
├── Solution Items/
│   ├── .editorconfig
│   └── Directory.Build.props
├── src/
│   ├── API/
│   │   └── Evently.Api/                        # Composition root — wires modules, no business logic
│   │       Program.cs
│   │       Modules/
│   │         ModuleExtensions.cs               # Extension methods to register each module
│   │       appsettings.json
│   │
│   ├── Common/
│   │   ├── Evently.Common.Domain/              # Shared domain primitives
│   │   │   EventId.cs                          # Strongly-typed event ID
│   │   │   Result.cs                           # Result / Error types
│   │   │   BaseEntity.cs                       # Base entity with domain events
│   │   │   IDomainEvent.cs                     # Domain event marker
│   │   │
│   │   ├── Evently.Common.Application/         # Shared application abstractions
│   │   │   EventBus/
│   │   │     IIntegrationEvent.cs              # Integration event marker interface
│   │   │     IEventBus.cs                      # In-process or broker abstraction
│   │   │   DateTimeProvider/
│   │   │     IDateTimeProvider.cs
│   │   │
│   │   ├── Evently.Common.Infrastructure/      # Shared infrastructure concerns
│   │   │   EventBus/
│   │   │     InProcessEventBus.cs              # Default in-process implementation
│   │   │   BackgroundJobs/
│   │   │     IntegrationEventProcessor.cs
│   │   │
│   │   └── Evently.Common.Presentation/        # Shared presentation concerns
│   │       Results/
│   │         ResultExtensions.cs               # Map Result to IResult / ProblemDetails
│   │       ExceptionHandling/
│   │         GlobalExceptionHandler.cs
│   │
│   └── Modules/
│       ├── Attendance/                         # Attendance bounded context
│       │   ├── Evently.Modules.Attendance.Domain/
│       │   │   Attendee.cs                     # Internal: aggregate root / entity
│       │   │   Events/
│       │   │     AttendanceRegistered.cs       # Internal: domain event
│       │   │
│       │   ├── Evently.Modules.Attendance.Application/
│       │   │   Attendees/
│       │   │     GetAttendee/
│       │   │       GetAttendeeQuery.cs
│       │   │       GetAttendeeHandler.cs
│       │   │     RegisterAttendee/
│       │   │       RegisterAttendeeCommand.cs
│       │   │       RegisterAttendeeHandler.cs
│       │   │   IntegrationEventsHandlers/      # Handles events from other modules
│       │   │     EventCreatedHandler.cs
│       │   │
│       │   ├── Evently.Modules.Attendance.Infrastructure/
│       │   │   Database/
│       │   │     AttendanceDbContext.cs        # Internal: own DbContext, own schema
│       │   │     Configurations/
│       │   │       AttendeeConfiguration.cs
│       │   │   DependencyInjection.cs          # Internal service registrations
│       │   │
│       │   ├── Evently.Modules.Attendance.Presentation/
│       │   │   Attendees/
│       │   │     GetAttendeeEndpoint.cs
│       │   │     RegisterAttendeeEndpoint.cs
│       │   │   AttendanceModule.cs             # Public: MapGroup + endpoint wiring
│       │   │
│       │   ├── Evently.Modules.Attendance.IntegrationEvents/  # Public contracts
│       │   │   AttendanceRegisteredIntegrationEvent.cs
│       │   │   AttendanceDto.cs
│       │   │
│       │   └── test/
│       │       ├── Evently.Modules.Attendance.ArchitectureTests/
│       │       ├── Evently.Modules.Attendance.IntegrationTests/
│       │       └── Evently.Modules.Attendance.UnitTests/
│       │
│       ├── Events/                             # Events bounded context (same 5-project layout)
│       ├── Ticketing/                          # Ticketing bounded context
│       └── Users/                              # Users bounded context
│
├── test/
│   ├── Evently.ArchitectureTests/              # Cross-cutting architecture rules
│   └── Evently.IntegrationTests/               # End-to-end integration tests
```

### Project Dependencies Per Module

Every module follows the same dependency flow:

```
                        ┌──────────────────────┐
                        │  .IntegrationEvents   │  ← Public contracts only
                        │  (no dependencies)    │
                        └──────────┬───────────┘
                                   │ referenced by other modules
      ┌────────────────────────────┼──────────────────────────────┐
      ▼                            ▼                              ▼
┌──────────────┐  ┌──────────────────┐  ┌────────────────────────┐
│  .Domain      │  │  .Application    │  │  .Presentation         │
│              │◄─┤                  │◄─┤                        │
│ Common.Domain│  │ Common.App       │  │ Common.Presentation    │
└──────┬───────┘  └───────┬──────────┘  └────────────────────────┘
       │                  │
       │          ┌───────▼──────────┐
       └─────────►│  .Infrastructure │
                  │                  │
                  │ Common.Infra     │
                  └──────────────────┘
```

- **Domain** → references `Common.Domain` only
- **Application** → references `Domain` + `Common.Application`
- **Infrastructure** → references `Application` + `Domain` + `Common.Infrastructure`
- **Presentation** → references `Application` + `Common.Presentation`
- **IntegrationEvents** → no project references (pure POCO contracts)

### Module Registration (Composition Root)

Each module exposes extension methods via its Presentation project. The API host never touches module internals.

```csharp
// Evently.Modules.Attendance.Presentation/AttendanceModule.cs
public static class AttendanceModule
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Infrastructure layer registers its own services via DependencyInjection.cs
        services.AddAttendanceInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapAttendanceModule(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance").WithTags("Attendance");

        group.MapGet("/attendees/{id:guid}", GetAttendeeEndpoint.Handle);
        group.MapPost("/attendees", RegisterAttendeeEndpoint.Handle);

        return app;
    }
}

// Evently.Modules.Attendance.Infrastructure/DependencyInjection.cs
internal static class DependencyInjection
{
    public static IServiceCollection AddAttendanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AttendanceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                o => o.MigrationsHistoryTable("__EFMigrationsHistory", "attendance")));

        services.AddScoped<IAttendanceQueryService, AttendanceQueryService>();

        return services;
    }
}

// Evently.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAttendanceModule(builder.Configuration);
builder.Services.AddEventsModule(builder.Configuration);
builder.Services.AddTicketingModule(builder.Configuration);
builder.Services.AddUsersModule(builder.Configuration);

var app = builder.Build();

app.MapAttendanceModule();
app.MapEventsModule();
app.MapTicketingModule();
app.MapUsersModule();

app.Run();
```

### Schema-Per-Module DbContext

Each module uses its own schema within the same database. The DbContext lives in the Infrastructure project and is `internal`.

```csharp
// Evently.Modules.Attendance.Infrastructure/Database/AttendanceDbContext.cs
internal sealed class AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
    : DbContext(options)
{
    public DbSet<Attendee> Attendees => Set<Attendee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("attendance");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AttendanceDbContext).Assembly);
    }
}

// Migration command (run from the Infrastructure project):
// dotnet ef migrations add InitialCreate --context AttendanceDbContext \
//   --output-dir Database/Migrations \
//   --project Evently.Modules.Attendance.Infrastructure
```

### Integration Events

Integration events are the only public contracts between modules. They live in the dedicated `.IntegrationEvents` project — a separate class library with no dependencies.

```csharp
// Evently.Modules.Attendance.IntegrationEvents/AttendanceRegisteredIntegrationEvent.cs
public sealed record AttendanceRegisteredIntegrationEvent(
    Guid EventId,
    Guid AttendeeId,
    Guid EventId,
    DateTimeOffset OccurredAt);
```

The Application layer of a module contains handlers for integration events published by other modules:

```csharp
// Evently.Modules.Attendance.Application/IntegrationEventsHandlers/EventCreatedHandler.cs
internal sealed class EventCreatedIntegrationEventHandler(
    AttendanceDbContext db,
    IDateTimeProvider dateTimeProvider)
{
    public async Task Handle(EventCreatedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        // React to event published by Events module
        // Attendance module never references Events module internals
    }
}
```

### Shared Common Kernel (Layered)

The Common project is split into 4 layers matching Clean Architecture concerns. It provides shared building blocks but contains **no business logic**.

```csharp
// Evently.Common.Domain/Result.cs
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

public class Result
{
    public bool IsSuccess { get; }
    public Error Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

// Evently.Common.Application/EventBus/IIntegrationEvent.cs
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

// Evently.Common.Application/EventBus/IEventBus.cs
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IIntegrationEvent;
}

// Evently.Common.Presentation/Results/ResultExtensions.cs
public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        return result.IsSuccess
            ? Results.Ok()
            : Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: result.Error.Message);
    }
}
```

### In-Process Event Bus

For monolith-first, an in-process event bus using `System.Threading.Channels`:

```csharp
// Evently.Common.Infrastructure/EventBus/InProcessEventBus.cs
public sealed class InProcessEventBus : IEventBus, IHostedService
{
    private readonly Channel<IIntegrationEvent> _channel =
        Channel.CreateBounded<IIntegrationEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    private readonly Dictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers = [];
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _cts;

    public InProcessEventBus(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IIntegrationEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var list))
        {
            list = [];
            _handlers[typeof(TEvent)] = list;
        }
        list.Add((@event, ct) => handler((TEvent)@event, ct));
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        await _channel.Writer.WriteAsync(@event, ct);
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = ProcessEvents(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        _channel.Writer.TryComplete();
        return Task.CompletedTask;
    }

    private async Task ProcessEvents(CancellationToken ct)
    {
        await foreach (var @event in _channel.Reader.ReadAllAsync(ct))
        {
            if (_handlers.TryGetValue(@event.GetType(), out var handlers))
            {
                using var scope = _scopeFactory.CreateScope();
                foreach (var handler in handlers)
                {
                    await handler(@event, ct);
                }
            }
        }
    }
}
```

### Testing Per Module

Each module has its own test projects, plus solution-level cross-cutting tests:

```
test/
  Evently.ArchitectureTests/              # Enforces module isolation rules globally
    ModuleIsolationTests.cs               # No module references another module's internals
    NamingConventionTests.cs

  Evently.IntegrationTests/               # End-to-end tests spanning multiple modules

Modules/Attendance/test/
  Evently.Modules.Attendance.ArchitectureTests/  # Module-internal architecture rules
    DomainLayerTests.cs                           # Domain has no external dependencies
    InfrastructureLayerTests.cs
  Evently.Modules.Attendance.IntegrationTests/   # Module-level integration tests
    RegisterAttendeeTests.cs
  Evently.Modules.Attendance.UnitTests/          # Unit tests for domain/application logic
    AttendeeTests.cs
```

### Adding a New Module

When adding a new bounded context (e.g., `Notifications`):

1. **Create 5 projects** under `src/Modules/Notifications/`:
   - `Evently.Modules.Notifications.Domain`
   - `Evently.Modules.Notifications.Application`
   - `Evently.Modules.Notifications.Infrastructure`
   - `Evently.Modules.Notifications.Presentation`
   - `Evently.Modules.Notifications.IntegrationEvents`

2. **Set project references** following the dependency flow (see diagram above).

3. **Create test projects** under `src/Modules/Notifications/test/`:
   - `Evently.Modules.Notifications.ArchitectureTests`
   - `Evently.Modules.Notifications.IntegrationTests`
   - `Evently.Modules.Notifications.UnitTests`

4. **Add the DbContext** in Infrastructure with its own schema:
   ```csharp
   internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
       : DbContext(options)
   {
       protected override void OnModelCreating(ModelBuilder modelBuilder)
       {
           modelBuilder.HasDefaultSchema("notifications");
           modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
       }
   }
   ```

5. **Create the module entry point** in Presentation:
   ```csharp
   public static class NotificationsModule
   {
       public static IServiceCollection AddNotificationsModule(
           this IServiceCollection services, IConfiguration configuration)
       {
           services.AddNotificationsInfrastructure(configuration);
           return services;
       }

       public static IEndpointRouteBuilder MapNotificationsModule(
           this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/notifications").WithTags("Notifications");
           // Map endpoints...
           return app;
       }
   }
   ```

6. **Register in the API host**:
   ```csharp
   // Evently.Api/Program.cs
   builder.Services.AddNotificationsModule(builder.Configuration);
   app.MapNotificationsModule();
   ```

### Extracting a Module to a Microservice

When a module needs independent deployment:

1. **Replace in-process event bus** with a message broker (RabbitMQ, Azure Service Bus) using Wolverine or MassTransit. See the **messaging** skill.
2. **Replace direct contract references** with HTTP client or gRPC calls for synchronous queries. See the **httpclient-factory** skill.
3. **Move the module to its own repository** (or keep in monorepo with separate deployment).
4. **Give it its own database** — migrate from schema-per-module to a separate database instance.
5. **Update the API host** — remove the module registration, replace with HTTP client or service reference.

```csharp
// Before: in-process module
builder.Services.AddAttendanceModule(builder.Configuration);
app.MapAttendanceModule();

// After: remote service
builder.Services.AddHttpClient<IAttendanceQueryService, AttendanceServiceClient>(
    client => client.BaseAddress = new("https://attendance-service.internal"));
```

## Anti-patterns

### Sharing an Entity Across Modules

```csharp
// BAD — both modules reference the same entity class
// Evently.Common.Domain/Attendee.cs
public class Attendee { ... }

// Attendance and Events modules both use Attendee directly
// Now they're coupled at the data and schema level

// GOOD — each module has its own model
// Attendance module: owns Attendee as a domain entity in .Domain project
// Events module: only knows about AttendeeId (a Guid or strongly-typed ID)
// If Events needs attendee details, it subscribes to integration events
```

### Cross-Module Database Queries

```csharp
// BAD — Attendance module directly queries Events tables
var events = await db.Set<Event>()  // Event is from Events module!
    .Where(e => eventIds.Contains(e.Id))
    .ToListAsync(ct);

// GOOD — Attendance module subscribes to Events module integration events
// and maintains its own read model populated from those events
```

### Fat Shared Kernel

```csharp
// BAD — Common project becomes a dumping ground
Evently.Common.Domain/
  Entities/          # domain entities don't belong here
  Services/          # business logic doesn't belong here
  Helpers/           # utility classes don't belong here

// GOOD — Common contains only building blocks
Evently.Common.Domain/          # Result, Error, BaseEntity, IDomainEvent
Evently.Common.Application/     # IIntegrationEvent, IEventBus, IDateTimeProvider
Evently.Common.Infrastructure/  # InProcessEventBus, integration event processor
Evently.Common.Presentation/    # Result-to-IResult mapping, exception handling
```

### Module Referencing Another Module's Internals

```csharp
// BAD — Attendance module references Events module's Application or Domain
// Evently.Modules.Attendance.Application references Evently.Modules.Events.Application
// Tight coupling — can't extract either module independently

// GOOD — Attendance only references Events.IntegrationEvents
// Evently.Modules.Attendance.Application references Evently.Modules.Events.IntegrationEvents
// The IntegrationEvents project has no dependencies — just POCO contracts
```

### Single DbContext for All Modules

```csharp
// BAD — one DbContext serving all modules
public class AppDbContext : DbContext
{
    public DbSet<Attendee> Attendees => Set<Attendee>();     // Attendance module
    public DbSet<Event> Events => Set<Event>();              // Events module
    public DbSet<Ticket> Tickets => Set<Ticket>();           // Ticketing module
}

// GOOD — each module has its own DbContext in its Infrastructure project
internal sealed class AttendanceDbContext : DbContext { /* attendance schema */ }
internal sealed class EventsDbContext : DbContext { /* events schema */ }
internal sealed class TicketingDbContext : DbContext { /* ticketing schema */ }
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| When to use Modular Monolith | Multiple bounded contexts, team-per-domain, or future microservices extraction |
| Schema-per-module vs separate databases | Start with schema-per-module (simpler ops), extract to separate DBs when scaling demands it |
| In-process vs message broker event bus | Start in-process; switch to Wolverine/MassTransit when you need reliability (outbox, retries) |
| Internal architecture per module | All modules use Clean Architecture (Domain → Application → Infrastructure/Presentation) for consistency |
| Common kernel contents | Domain primitives, event bus abstractions, shared presentation helpers. No business logic |
| How to handle cross-module queries | Subscribe to integration events and maintain local read models. Avoid synchronous cross-module queries |
| When to extract to microservices | When a module has independent scaling needs, different deployment cadence, or different SLA requirements |
| Module referencing another module | Only reference `.IntegrationEvents` projects. Never reference Domain, Application, or Infrastructure |
| Number of modules | Start with 2-4 coarse-grained modules aligned to business capabilities. Split when a module grows too large for its team |
| Module naming convention | `{Company}.Modules.{ModuleName}.{Layer}` — e.g., `Evently.Modules.Attendance.Domain` |
