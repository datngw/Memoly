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
- **Share links**: publish a memory as a read-only public link to show it to others, styled by the owner — colors, text, and more
- **Export & import**: full backups; transfer between devices

### Product Philosophy

- **One user, no social** — a single person's memories; no feeds, no profiles, no tracking
- **Share what you choose** — any memory can be published as a read-only link, styled your way, and revoked at any time
- **Well-made, not new** — the value is a beautiful, easy interface, not a novel concept
- **Voice comes first** — recordings are a main content type, equal to photos and videos
- **No lock-in** — everything can be exported and re-imported at any time

## Architecture

**Vertical Slice Architecture** — one application, one project, organized by feature slices instead of layers. The React SPA is the Phase 1 client.

Feature areas: **Memories** (entries, media, and search), **Collections**, **Sharing** (share links), and **Backup** (export & import) — each a folder of feature slices under `Features/`. Search is a product feature delivered as query slices inside `Memories`, not a separate feature folder. Authentication is a host-level concern in `Shared/Auth/`.

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
│   │   └── Memoly.Api/                        # The whole application — one project (registered in AppHost)
│   │       ├── Program.cs                     # Composition root: services, Wolverine, endpoint mapping
│   │       ├── Features/                      # Vertical slices — all app functionality lives here
│   │       │   ├── Memories/                  # Entries + media + search slices
│   │       │   ├── Collections/
│   │       │   ├── Sharing/                   # Share links
│   │       │   └── Backup/                    # Export & import
│   │       ├── Shared/                        # Building blocks — zero business logic
│   │       │   ├── Database/                  # MemolyDbContext + migrations
│   │       │   ├── Media/                     # Filesystem media storage + thumbnails
│   │       │   ├── Auth/                      # Keycloak OIDC setup (host-level)
│   │       │   ├── Domain/                    # Result, Error, BaseEntity, event abstractions
│   │       │   └── Presentation/              # Result→IResult mapping, exception handling
│   │       └── appsettings.json
│   │
│   └── Frontend/
│       └── memoly-web/                        # React + Vite SPA (Phase 1 client)
│
├── test/
│   ├── Memoly.ArchitectureTests/              # Architecture rules (slice isolation, Shared purity)
│   ├── Memoly.IntegrationTests/               # WebApplicationFactory + Testcontainers (Postgres)
│   └── Memoly.UnitTests/                      # Handler and domain-logic unit tests
│
├── specs/
│   └── ###-feature-name/
│
└── docs/
```

### Inside the Application — Vertical Slice Architecture

The application is **one project** organized by feature, not by layer:

- **Feature slices** — `Features/{Feature}/{Slice}/` folders. A slice is one vertical cut of one operation: endpoint (`MapGroup` + `MapGet`/`MapPost`), Wolverine handler, FluentValidation validator, and DTOs. A slice owns its code — nothing outside it references the slice's internals.
- **Feature folder** — `Features/{Feature}/` groups its slices, the feature's EF Core configuration, and its `{Feature}Endpoints.cs` route group.
- **Shared building blocks** — `Shared/`: the single `MemolyDbContext` + migrations, media storage helpers, auth setup, Result/presentation helpers. No business logic.
- **No layer projects** — no Domain/Application/Infrastructure split; each slice carries everything it needs from endpoint to EF Core configuration.

Example feature layout:

```
Features/Memories/
├── CreateMemory/
│   ├── CreateMemoryEndpoint.cs        # MapPost("/api/memories"), maps Result → IResult
│   ├── CreateMemoryHandler.cs         # Wolverine command handler
│   ├── CreateMemoryValidator.cs       # FluentValidation rules
│   └── CreateMemoryRequest.cs         # Request/response DTOs
├── GetMemory/
├── ListMemories/
├── MemoryConfiguration.cs             # EF Core IEntityTypeConfiguration for the feature's entities
└── MemoriesEndpoints.cs               # Maps the feature's route group: MapMemoriesEndpoints(app)
```

### Slice Isolation Rules

- Slices never reference each other — no cross-slice handler calls, no shared query services between slices.
- Any slice may **read** any table through the shared `MemolyDbContext`; each feature's EF Core configuration and write paths stay in the feature that owns the entity.
- Cross-feature **side effects** go through Wolverine events (e.g., `MemoryDeleted` → media cleanup handler deletes the files) — never by writing to another feature's tables directly.
- Code needed by multiple slices moves to `Shared/` — and `Shared/` stays free of business logic.
- Feature folders are `internal` to `Memoly.Api`; test projects get access via `InternalsVisibleTo`.

### Composition Root (Program.cs)

The API host wires everything: shared services, Wolverine, EF Core, auth. Handlers are discovered automatically from the application assembly — no per-module registration:

```csharp
// Shared/DependencyInjection.cs — internal registrations
internal static IServiceCollection AddMemoly(this IServiceCollection services, IConfiguration configuration);

// Memoly.Api: Program.cs — thin wiring
builder.Services.AddMemoly(builder.Configuration);
builder.Host.UseWolverine(opts =>
{
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();    // entities + durable messages commit atomically
});

var app = builder.Build();
app.MapMemoriesEndpoints();                  // one line per feature group
app.MapSharingEndpoints();
app.Run();
```

Wolverine runs **in-process** — no message broker. Events are published as cascading return values or via `IMessageBus`; events whose side effects must survive a crash (e.g., media cleanup) use EF Core durable messaging so the message commits with the data.

### Media Handling

Photos, videos, and audio recordings are large binary content and need explicit treatment:

- Media files are stored on the server filesystem under a configurable root (Options pattern, helpers in `Shared/Media/`) — never as byte columns in the database; the DB keeps metadata + file references.
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
- **API**: ASP.NET Core Minimal APIs with OpenAPI
- **ORM**: EF Core with code-first migrations (single `MemolyDbContext`)
- **API Docs**: Scalar
- **Logging**: Serilog (two-stage bootstrap, structured logging) + Seq
- **Resilience**: Polly v8 pipelines
- **CQRS & events**: Wolverine — in-process command bus and event handling (replaces MediatR + MassTransit); EF Core durable messaging so events commit with the data; no message broker
- **Validation**: FluentValidation
- **Mapping**: Mapster
- **Authentication**: Keycloak (OIDC identity provider, run as a container in dev; single realm, single user — no sign-up flow, no multi-tenancy)
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

# EF Core migrations
dotnet ef migrations add InitialCreate --context MemolyDbContext \
  --output-dir Shared/Database/Migrations \
  --project src/API/Memoly.Api
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
- Domain events are `sealed record` types
- Wolverine handler conventions: plain `Handle`/`HandleAsync` methods (no mediator interfaces), events published as cascading return values or via `IMessageBus` — never fire-and-forget; events with side effects that must survive a crash go through EF Core durable messaging
- Accessibility is a feature: keyboard navigation, focus states, reduced-motion variants, dark mode — every SPA screen; responsive from phone width up

## Adding a New Feature

1. Create `Features/{Feature}/{Slice}/` folders — one per operation: endpoint, Wolverine handler, validator, DTOs
2. Add the feature's EF Core configuration (`IEntityTypeConfiguration`) and apply it in `MemolyDbContext`; add a migration if the schema changed
3. Add `{Feature}Endpoints.cs` with the feature's `MapGroup`, and map it in `Program.cs`
4. Any cross-feature reaction is a Wolverine event handled in the reacting feature
5. Add tests (unit for handlers/domain logic, integration for endpoints)
6. Add feature folder in `src/Frontend/memoly-web/src/features/{name}/` with components, hooks, and api subfolders

## Specify Integration

The `.specify/` directory contains project governance:

- **Constitution** (`.specify/memory/constitution.md`): Authoritative source for architectural decisions
- **Templates** (`.specify/templates/`): Plan, spec, task, and checklist templates for structured development workflow

Feature work should follow the Specify workflow: spec → plan → tasks → implementation.
