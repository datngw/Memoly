# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Status

Memoly is a greenfield project. The codebase has not been scaffolded yet — no `.slnx`, `.csproj`, or source files exist. Governance is defined in `.specify/memory/constitution.md`.

**Phase plan**:

- **Phase 1 (current)** — **web**: React SPA + .NET backend API, the full Memoly experience in the browser.
- **Phase 2 (future)** — native iOS client consuming the same API. Do not build iOS code now.

## Product Overview

**Memoly** keeps life's moments together — photos, videos, voice recordings, and writing — each stored with the feeling, place, and story that go with it, so nothing gets lost over the years.

### Core Features

- **Four kinds of entries**: photos, videos, voice recordings, and writing
- **Context per entry**: mood, place, and story alongside the media
- **Collections**: group entries by theme
- **Search**: find old entries quickly
- **Time Capsules**: entries hidden until a chosen date
- **Share links**: publish a memory as a read-only public link to show it to others, styled by the owner — colors, text, and more
- **Export & import**: full backups; transfer between devices

### Product Philosophy

- **One user, no social** — a single person's memories; no feeds, no profiles, no tracking
- **Share what you choose** — any memory can be published as a read-only link, styled your way, and revoked at any time
- **Well-made, not new** — the value is a beautiful, easy interface, not a novel concept
- **Voice comes first** — recordings are a main content type, equal to photos and videos
- **No lock-in** — everything can be exported and re-imported at any time

## Architecture

**Modular Monolith** — each bounded context is an independent module with 5 projects following Clean Architecture internally. The React SPA is the Phase 1 client.

Candidate bounded contexts (finalized per feature spec): **Memories** (entries + media), **Collections**, **Time Capsules**, **Backup** (export/import), plus authentication.

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
│   │   ├── Memoly.Common.Application/         # Shared app abstractions (IIntegrationEvent, IDateTimeProvider)
│   │   ├── Memoly.Common.Infrastructure/      # Shared infra (Wolverine configuration helpers, outbox support)
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
│       └── memoly-web/                        # React + Vite SPA (Phase 1 client)
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
- Each module has its own `DbContext` (own schema via `modelBuilder.HasDefaultSchema("modulename")`), owned by its Infrastructure project. Contexts are registered with `AddDbContextWithWolverineIntegration` and carry the Wolverine inbox/outbox tables so integration events commit with the data.
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

Wolverine is wired once at the API host — RabbitMQ transport, EF Core transactional outbox, auto-applied transactions. Each module's handlers are discovered by including its Presentation assembly:

```csharp
// API: Program.cs — Wolverine wiring alongside module registration
builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof({Name}Module).Assembly);  // repeat per module
    opts.UseRabbitMq(rabbit => rabbit.HostName = builder.Configuration["RabbitMq:Host"]!)
        .AutoProvision();                     // dev convenience — auto-create topology
    opts.Policies.AutoApplyTransactions();    // entities + outbox messages commit atomically
});
```

### Media Handling

Photos, videos, and audio recordings are large binary content and need explicit treatment:

- Media files are stored on the server filesystem under a configurable root (Options pattern), owned by the module that owns the content — never as byte columns in the database; the DB keeps metadata + file references.
- Uploads and downloads stream: multipart streaming in, range-request-capable endpoints out (needed for audio/video seeking). Never load a whole media file into memory.
- Deleting a memory must delete its media files (event-driven cleanup).
- The SPA records audio with `MediaRecorder` and uploads the blob; playback streams from the API.
- Generate downscaled thumbnails for grids/lists.

### Share Links

Any memory can be published as a public, read-only link — a way to show one moment to anyone who receives it:

- The API exposes an unauthenticated route group for shared pages. A share is addressed by an unguessable token — never by the memory's real id.
- A shared page renders exactly one memory (media, story, mood, place) and nothing else — no way to navigate into the rest of the app.
- The owner customizes the page's look — colors, text, and other presentation options defined per spec. Customization lives with the share, so the private entry stays unaffected.
- Unsharing revokes the token immediately; re-sharing issues a new token.
- Shared pages carry no tracking of any kind.

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
- **Messaging & CQRS**: Wolverine — one library for the command bus and integration events (replaces MediatR + MassTransit); RabbitMQ transport, transactional outbox with EF Core
- **Validation**: FluentValidation
- **Mapping**: Mapster
- **Authentication**: OpenIddict (secures a single-user deployment — no sign-up flow, no multi-tenancy)
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
- Wolverine handler conventions: plain `Handle`/`HandleAsync` methods (no mediator interfaces), events published as cascading return values or via `IMessageBus` — always through the transactional outbox, never fire-and-forget
- Accessibility is a feature: keyboard navigation, focus states, reduced-motion variants, dark mode — every SPA screen; responsive from phone width up

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
