<!--
Sync Impact Report
==================
Version change: (new) → 1.0.0
Modified principles: N/A (initial ratification)
Added sections:
  - Core Principles (5 principles)
  - Technology Stack
  - Development Workflow
  - Governance
Removed sections: N/A
Templates requiring updates:
  - .specify/templates/plan-template.md — ✅ no changes needed (generic placeholders compatible)
  - .specify/templates/spec-template.md — ✅ no changes needed (generic placeholders compatible)
  - .specify/templates/tasks-template.md — ✅ no changes needed (generic placeholders compatible)
  - .specify/templates/checklist-template.md — ✅ no changes needed
Follow-up TODOs: None
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
- Hot paths MUST avoid allocations where feasible: use `Span<T>`,
  `Memory<T>`, object pooling, and `ArrayPool<T>` for high-throughput
  scenarios.
- Database queries MUST be optimized: projection selects, compiled queries,
  batch operations, and proper indexing. N+1 queries are forbidden.
- Memory and latency budgets MUST be defined for critical paths. Any
  regression beyond budget requires investigation before merge.
- Use `ref struct`, `stackalloc`, and pooling patterns for
  performance-sensitive code paths where benchmarks justify the complexity.

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

- YAGNI: Do not build for hypothetical future requirements. Implement what
  the current feature requires.
- Prefer built-in .NET abstractions over third-party packages unless the
  package provides significant, proven value (e.g., EF Core, Serilog).
- Avoid premature abstraction: three similar lines of code are better than
  a wrong abstraction. Introduce abstractions when duplication has a clear
  pattern AND a third use case is imminent.
- Every architectural decision that adds complexity MUST be documented with
  rationale in the plan's Complexity Tracking table.
- Prefer vertical slice organization within modules over deep inheritance
  hierarchies.

**Rationale**: Unnecessary complexity is the primary source of maintenance
burden and onboarding friction. Simplicity enables velocity.

## Technology Stack

**Runtime**: .NET 10 (latest LTS features, C# 14)
**Web Framework**: ASP.NET Core Minimal APIs with OpenAPI support
**ORM**: Entity Framework Core with code-first migrations
**Logging**: Serilog with structured logging, two-stage bootstrap
**Testing**: xUnit v3, WebApplicationFactory, Testcontainers, Verify
**Architecture**: Modular Monolith
**API Documentation**: Scalar (replaces Swagger UI)
**Resilience**: Polly v8 pipelines (retry, circuit breaker, timeout)
**Package Management**: Central package management (Directory.Packages.props)
**Solution Format**: .slnx

## Development Workflow

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
- Any deviation from these principles MUST be explicitly justified in
  the plan's Complexity Tracking table.
- Constitution version follows semantic versioning: MAJOR for breaking
  principle changes, MINOR for new principles, PATCH for clarifications.
- Use `.specify/` templates and CLAUDE.md for runtime development guidance.

**Version**: 1.0.0 | **Ratified**: 2026-04-08 | **Last Amended**: 2026-04-08
