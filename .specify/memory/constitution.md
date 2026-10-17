<!--
Sync Impact Report
==================
Version change: 1.0.0 → 2.0.0 (product pivot; technical principles retained)
Modified principles:
  - None — principles I–V are unchanged from 1.0.0
Added sections:
  - Core Principle VI: "Privacy & User Data Ownership" (product-level, incl. the
    share-link rule: explicit per-memory sharing, read-only, revocable)
  - Phase Context under Development Workflow (web first, native iOS client later)
Removed sections: N/A
Templates requiring updates:
  - .specify/templates/plan-template.md — ✅ no changes needed (generic placeholders compatible)
  - .specify/templates/spec-template.md — ✅ no changes needed (generic placeholders compatible)
  - .specify/templates/tasks-template.md — ✅ no changes needed (generic placeholders compatible)
  - .specify/templates/checklist-template.md — ✅ no changes needed
Follow-up TODOs:
  - docs/architecture.png depicts the modular monolith architecture — still accurate; revisit if module layout changes
-->

# Memoly Constitution

## Core Principles

### I. Modular Architecture

Every feature MUST be organized as an independent module with clear boundaries.

- Each module MUST have its own layered structure (Application, Domain,
  Infrastructure, Presentation, IntegrationEvents) following the
  modular monolith pattern.
- Modules MUST communicate exclusively through well-defined integration
  events — no direct cross-module database queries or shared internal types.
- Every module MUST be independently buildable and testable in isolation.
- Shared kernel MUST be minimal: only common abstractions, value object
  types, and integration event contracts belong in shared code.

**Rationale**: Modular isolation enables independent development, testing,
and future extraction to microservices without refactoring.

### II. High Performance

Performance MUST be a first-class concern from design through implementation.

- All I/O-bound operations MUST use async/await with `CancellationToken`
  propagation end-to-end.
- Media (photos, videos, audio) MUST be streamed — never loaded fully into
  memory — for both upload and download, with range-request support for
  audio/video playback. Media files live on the filesystem, not in the
  database.
- Database queries MUST be optimized: projection selects, compiled queries,
  batch operations, and proper indexing. N+1 queries are forbidden.
- Memory and latency budgets MUST be defined for critical paths (memory
  grid scrolling, media streaming). Any regression beyond budget requires
  investigation before merge.
- Hot paths MUST avoid allocations where feasible: use `Span<T>`,
  `Memory<T>`, object pooling, and `ArrayPool<T>` for high-throughput
  scenarios.

**Rationale**: Performance regressions are exponentially harder to fix than
prevent. Establishing budgets and discipline early avoids costly rewrites.

### III. Best Practices & Code Quality

All code MUST follow established .NET best practices and modern idiomatic C#.

- SOLID principles MUST be followed: single responsibility, open/closed,
  Liskov substitution, interface segregation, dependency inversion.
- Use modern C# 14 / .NET 10 idioms: primary constructors, collection
  expressions, file-scoped namespaces, global usings, pattern matching,
  raw string literals, and the `field` keyword where appropriate.
- Dependency injection MUST use constructor injection. Avoid service
  locator pattern. Register services with explicit lifetimes
  (Scoped/Transient/Singleton) with documented rationale for non-Transient.
- Error handling MUST use the Result pattern for domain/application layer
  and ProblemDetails (RFC 9457) for API responses. Exceptions are reserved
  for truly exceptional conditions.
- Configuration MUST use the Options pattern with `IOptions<T>` or
  `IOptionsSnapshot<T>`. No direct `IConfiguration` injection beyond
  startup.

**Rationale**: Consistent adherence to best practices reduces cognitive load,
minimizes bugs, and makes the codebase approachable for any .NET developer.

### IV. Test-First Development

Tests MUST be written before implementation. This is non-negotiable.

- TDD cycle: Write tests → User approves → Verify tests fail → Implement
  → Verify tests pass → Refactor.
- Unit tests cover domain logic and application services in isolation.
- Integration tests cover module contracts, database interactions, and
  cross-boundary communication using real infrastructure (Testcontainers).
- Contract tests validate API endpoints against published OpenAPI specs.
- Test naming MUST follow the `Method_Scenario_ExpectedResult` convention.
- All tests MUST be deterministic: no time-dependent, random, or
  network-dependent unit tests. Use mocks/fakes for external dependencies.

**Rationale**: Test-first development catches defects at design time, forces
clean interfaces, and provides living documentation of intended behavior.

### V. Simplicity & Pragmatism

Start simple. Add complexity only when justified by measurable need.

- YAGNI: Do not build for hypothetical future requirements — including the
  Phase 2 iOS client — implement what the current feature requires.
- Prefer built-in .NET abstractions over third-party packages unless the
  package provides significant, proven value (e.g., EF Core, Serilog).
- Avoid premature abstraction: three similar lines of code are better than a
  wrong abstraction. Introduce abstractions when duplication has a clear
  pattern AND a third use case is imminent.
- Every architectural decision that adds complexity MUST be documented with
  rationale in the plan's Complexity Tracking table.
- Prefer vertical slice organization within modules over deep inheritance
  hierarchies.

**Rationale**: Unnecessary complexity is the primary source of maintenance
burden and onboarding friction. Simplicity enables velocity.

### VI. Privacy & User Data Ownership

Memoly holds a person's private memories. The product MUST treat that data
as the user's property.

- No telemetry, analytics, or tracking of any kind.
- No social network features: no feeds, no profiles, no following, no
  cross-user browsing.
- Sharing is explicit and scoped: a user MAY publish a single memory as a
  public link. A shared page is read-only, shows exactly one memory, and
  can be revoked at any time.
- Export/import is a first-class feature: users MUST always be able to get
  a complete, self-contained archive of their data (entries + media) and
  restore it.
- Third-party services MUST NOT receive user content.

**Rationale**: Trust is the foundation of a memory-keeping product. Every
feature decision must be weighed against "would the user feel their
memories are still theirs?"

## Technology Stack

**Runtime**: .NET 10 (latest LTS features, C# 14)
**Web Framework**: ASP.NET Core Minimal APIs with OpenAPI support
**ORM**: Entity Framework Core with code-first migrations
**Messaging & CQRS**: Wolverine (RabbitMQ transport, transactional outbox with EF Core)
**Logging**: Serilog with structured logging, two-stage bootstrap
**Testing**: xUnit v3, WebApplicationFactory, Testcontainers, Verify
**Architecture**: Modular Monolith
**API Documentation**: Scalar (replaces Swagger UI)
**Resilience**: Polly v8 pipelines (retry, circuit breaker, timeout)
**Package Management**: Central package management (Directory.Packages.props)
**Solution Format**: .slnx
**Frontend**: React + Vite, Zustand, shadcn/ui, Tailwind CSS

## Development Workflow

### Phase Context

- Phase 1 (current): web — React SPA + .NET backend API.
- Phase 2 (future): native iOS client consuming the same API.

### Branch Strategy

- Feature branches follow `###-feature-name` sequential numbering.
- All work MUST reference a feature spec in `/specs/###-feature-name/`.

### Quality Gates

1. All tests MUST pass before merge (unit, integration, contract).
2. No compiler warnings in release builds (`TreatWarningsAsErrors` for
   production code).
3. Code review MUST verify constitution compliance.
4. Performance-critical changes MUST include benchmark results.

### Code Organization

- Each module owns its data and exposes functionality through its
  Application layer.
- Integration events are the only cross-module communication mechanism.
- Shared kernel contains zero business logic.

## Governance

This constitution is the authoritative source for architectural and
development decisions on the Memoly project.

- All PRs and code reviews MUST verify compliance with these principles.
- Amendments require: written proposal, documented rationale, and a
  migration plan for existing code that violates the new principle.
- Any deviation from these principles MUST be explicitly justified in the
  plan's Complexity Tracking table.
- Constitution version follows semantic versioning: MAJOR for breaking
  principle changes, MINOR for new principles, PATCH for clarifications.
- Use `.specify/` templates and CLAUDE.md for runtime development guidance.

**Version**: 2.0.0 | **Ratified**: 2026-04-08 | **Last Amended**: 2026-10-17
