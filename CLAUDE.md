# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Status

Memoly is a greenfield project. The codebase has not been scaffolded yet — no `.slnx`, `.csproj`, or source files exist. Governance is defined in `.specify/memory/constitution.md`.

## Architecture

**Modular Monolith** — each bounded context is an independent module with 5 projects following Clean Architecture internally.

### Solution Structure

```
Memoly/
├── .editorconfig
├── Directory.Build.props
├── Directory.Packages.props
├── Memoly.slnx
│
├── src/
│   ├── Aspire/
│   │   ├── Memoly.AppHost/                    # .NET Aspire orchestration root
│   │   │   ├── Program.cs                     # AddProject, AddNpgsql, AddContainer for dev environment
│   │   │   └── appsettings.json
│   │   └── Memoly.ServiceDefaults/            # Shared Aspire service configuration
│   │       └── Extensions.cs                  # OpenTelemetry, health checks, resilience, service discovery
│   │
│   ├── API/
│   │   └── Memoly.Api/                        # Minimal API host (registered in AppHost)
│   │       ├── Program.cs                     # Calls AddServiceDefaults() + registers modules
│   │       ├── Modules/
│   │       │   └── ModuleExtensions.cs         # Extension methods to register each module
│   │       └── appsettings.json
│   │
│   ├── Common/
│   │   ├── Memoly.Common.Domain/              # Shared domain primitives (Result, Error, BaseEntity, IDomainEvent)
│   │   ├── Memoly.Common.Application/         # Shared app abstractions (IIntegrationEvent, IEventBus, IDateTimeProvider)
│   │   ├── Memoly.Common.Infrastructure/      # Shared infra (InProcessEventBus, IntegrationEventProcessor)
│   │   └── Memoly.Common.Presentation/        # Shared presentation (Result→IResult mapping, exception handling)
│   │
│   ├── Modules/
│   │   └── {ModuleName}/                       # Each bounded context
│   │       ├── Memoly.Modules.{Name}.Domain/
│   │       ├── Memoly.Modules.{Name}.Application/
│   │       ├── Memoly.Modules.{Name}.Infrastructure/
│   │       ├── Memoly.Modules.{Name}.Presentation/
│   │       └── Memoly.Modules.{Name}.IntegrationEvents/  # Public contracts only (no dependencies)
│   │
│   └── Frontend/
│       └── memoly-web/                        # React + Vite SPA
│
├── test/
│   ├── Memoly.ArchitectureTests/              # Cross-cutting architecture rules (module isolation)
│   ├── Memoly.IntegrationTests/               # End-to-end tests spanning multiple modules
│   └── {ModuleName}/test/
│       ├── Memoly.Modules.{Name}.ArchitectureTests/
│       ├── Memoly.Modules.{Name}.IntegrationTests/
│       └── Memoly.Modules.{Name}.UnitTests/
│
├── specs/
│   └── ###-feature-name/
│
└── docs/
```

### Project Dependencies Per Module

```
                        ┌──────────────────────┐
                        │  .IntegrationEvents   │  ← Public contracts only (no dependencies)
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
                  │ Common.Infra     │
                  └──────────────────┘
```

- **Domain** → `Common.Domain` only
- **Application** → `Domain` + `Common.Application`
- **Infrastructure** → `Application` + `Domain` + `Common.Infrastructure`
- **Presentation** → `Application` + `Common.Presentation`
- **IntegrationEvents** → no project references (pure POCO contracts)

### Module Isolation Rules

- Module internal types are `internal` by default. Only `.IntegrationEvents` exposes public contracts.
- No module references another module's Domain, Application, or Infrastructure projects — only `.IntegrationEvents`.
- Each module has its own `DbContext` (own schema via `modelBuilder.HasDefaultSchema("modulename")`), owned by its Infrastructure project.
- Modules communicate exclusively through integration events — no direct cross-module DB queries.
- Common kernel contains zero business logic (only building blocks: Result types, event bus abstractions, presentation helpers).

### Module Registration (Composition Root)

Each module exposes extension methods via its Presentation project. The API host never touches module internals:

```csharp
// Presentation: {Name}Module.cs — public extension methods
public static IServiceCollection Add{Name}Module(this IServiceCollection services, IConfiguration configuration);
public static IEndpointRouteBuilder Map{Name}Module(this IEndpointRouteBuilder app);

// Infrastructure: DependencyInjection.cs — internal service registrations
internal static IServiceCollection Add{Name}Infrastructure(this IServiceCollection services, IConfiguration configuration);

// API: Program.cs — thin wiring
builder.Services.Add{Name}Module(builder.Configuration);
app.Map{Name}Module();
```

## Technology Stack

### Backend

- **Runtime**: .NET 10, C# 14
- **Orchestration**: .NET Aspire (AppHost + ServiceDefaults in `src/Aspire/`)
- **Reverse Proxy**: Traefik
- **API**: ASP.NET Core Minimal APIs with OpenAPI
- **ORM**: EF Core with code-first migrations (schema-per-module)
- **API Docs**: Scalar
- **Logging**: Serilog (two-stage bootstrap, structured logging) + Seq
- **Resilience**: Polly v8 pipelines
- **Event Bus**: MassTransit (RabbitMQ transport)
- **CQRS**: MediatR
- **Validation**: FluentValidation
- **Mapping**: Mapster
- **Authentication**: OpenIddict
- **Testing**: xUnit v3, Fluent Assertions, Moq, WebApplicationFactory, Testcontainers
- **Code Quality**: SonarQube
- **Solution format**: `.slnx`
- **Package management**: Central (`Directory.Packages.props`)

### Frontend

- **Framework**: React + Vite (located at `src/Frontend/memoly-web/`)
- **State Management**: Zustand
- **UI Components**: shadcn/ui
- **Icons**: Lucide Icons
- **Styling**: Tailwind CSS

## Build & Run Commands

Once the project is scaffolded:

```bash
# Backend
dotnet build                          # Build solution
dotnet test                           # Run all tests
dotnet test --filter "FullyQualifiedName~TestMethod"  # Run single test
dotnet run --project src/Aspire/Memoly.AppHost       # Run all services via Aspire

# Frontend
cd src/Frontend/memoly-web
npm install                           # Install dependencies
npm run dev                           # Start dev server (proxies /api to Aspire)
npm run generate-api                  # Auto-generate API client from OpenAPI spec

# EF Core migrations (run from the module's Infrastructure project)
dotnet ef migrations add InitialCreate --context {Module}DbContext \
  --output-dir Database/Migrations \
  --project src/Modules/{ModuleName}/Memoly.Modules.{Name}.Infrastructure
```

## Development Workflow

- Feature branches: `###-feature-name` (sequential numbering)
- Feature specs live in `/specs/###-feature-name/`
- TDD cycle: Write tests → User approves → Verify tests fail → Implement → Verify tests pass → Refactor
- `TreatWarningsAsErrors` enabled for production code
- Test naming: `Method_Scenario_ExpectedResult`

## Key Conventions

- Constructor injection only; no service locator pattern
- Result pattern for domain/application layer errors; ProblemDetails (RFC 9457) for API responses
- Options pattern (`IOptions<T>` / `IOptionsSnapshot<T>`) for configuration
- Async/await with `CancellationToken` propagation end-to-end
- Integration events are `sealed record` types implementing `IIntegrationEvent`

## Adding a New Module

1. Create 5 projects under `src/Modules/{Name}/` following the naming convention `Memoly.Modules.{Name}.{Layer}`
2. Set project references following the dependency diagram above
3. Create test projects (ArchitectureTests, IntegrationTests, UnitTests)
4. Add internal `DbContext` in Infrastructure with own schema (`HasDefaultSchema`)
5. Create public extension methods in Presentation (`Add{Name}Module`, `Map{Name}Module`)
6. Register in API host `Program.cs`
7. Add feature folder in `src/Frontend/memoly-web/src/features/{name}/` with components, hooks, and api subfolders

## Specify Integration

The `.specify/` directory contains project governance:

- **Constitution** (`.specify/memory/constitution.md`): Authoritative source for architectural decisions
- **Templates** (`.specify/templates/`): Plan, spec, task, and checklist templates for structured development workflow

Feature work should follow the Specify workflow: spec → plan → tasks → implementation.
