# Distributed Task Management API — Architecture

## Overview
- Goal: Build a production‑ready, cloud‑native Task Management REST API with clean architecture, strong testing posture, and pragmatic use of patterns.
- Non‑Goals: Over‑engineered DDD/microservices, bespoke infra; we favor YAGNI/KISS while conforming to SOLID and modern C#.

## Tech Stack (per assessment)
- Runtime: .NET 8 (ASP.NET Core Web API)
- Language: C# 12 with nullable reference types enabled
- Database: MongoDB 3.5.0 (official C# driver)
- Cache: Redis (StackExchange.Redis 2.9.32)
- Auth: Supabase JWT (validate via JWKS)
- Logging: Serilog 9.0.0 (structured, JSON)
- Health: ASP.NET Core HealthChecks
- Containers: Docker + Docker Compose
- Code Quality: .NET Analyzers (latest-recommended), StyleCop.Analyzers

## Solution Structure
```
src/
  TaskService.Api/            # API (endpoints, composition root)
  TaskService.Application/    # CQRS (commands/queries), handlers, validators
  TaskService.Domain/         # Entities, value objects, enums, domain services
  TaskService.Infrastructure/  # Mongo/Redis, repositories, external services
  TaskService.Application/Dtos/ # Versioned API request/response DTOs
  TaskService.Shared/         # Cross‑cutting (Result, errors, time, paging)
tests/
  TaskService.UnitTests/
  TaskService.IntegrationTests/
Dockerfile (API)
docker-compose.yml
.env.example
```

Rationale:
- Clear separation (SRP) and testability (DI, small seams). No "god" classes.
- DTOs in Application decouple external shapes from domain internals.
- CQRS‑lite: Commands mutate via write models, Queries read via read models where beneficial; same DB.

## Domain Model
- Project
  - Id (ObjectId)
  - OwnerId (string; Supabase user ID)
  - Name (string, required, 3..120)
  - Description (string?, <= 2k)
  - Status (enum: Active, Archived)
  - CreatedAt (Utc), UpdatedAt (Utc)
  - CreatedBy (string), UpdatedBy (string)
  - IsDeleted (bool; soft delete)

- Task
  - Id (ObjectId)
  - ProjectId (ObjectId)
  - Title (string, required, 3..160)
  - Description (string?, <= 4k)
  - Status (enum: New, InProgress, Blocked, Done)
  - Priority (enum: Low, Medium, High, Urgent)
  - AssigneeUserId (string?)
  - DueDate (DateTime?)
  - Tags (string[])
  - CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
  - IsDeleted (bool)

Notes:
- We store only Supabase user IDs (no PII). User profiles are derived via Supabase when needed.
- Audit fields provide minimal audit trail without full event sourcing.

## MongoDB Data Design
Collections
- `projects`
- `tasks`

Schema & Validation (driver‑side + FluentValidation)
- Required fields as above; string length constraints; enum whitelists.

Indexes
- projects
  - `{ OwnerId: 1, IsDeleted: 1, CreatedAt: -1 }` (query list)
  - `{ Name: 'text' }` (optional if search is needed; YAGNI if not used)
  - Partial unique (optional): `{ OwnerId: 1, Name: 1 }` with `partialFilterExpression: { IsDeleted: false }` to prevent duplicate active project names per owner while allowing soft-deleted duplicates. Safe to include; if timing is tight, defer.
- tasks
  - `{ ProjectId: 1, IsDeleted: 1, CreatedAt: -1 }`
  - `{ AssigneeUserId: 1, Status: 1 }`

Soft Deletes
- `IsDeleted: true`; repositories apply filters by default; admin/debug endpoints may opt‑in to include deleted.

Relationships
- Reference by IDs only: `Task.ProjectId`, `Task.AssigneeUserId`.

## API Design (v1)
Base: `http://localhost:5000`

Authentication (Supabase‑backed)
- POST `/api/auth/register` — proxy to Supabase sign‑up
- POST `/api/auth/login` — proxy to Supabase sign‑in, returns JWT
- GET `/api/auth/me` — read from JWT claims

Projects
- GET `/api/projects` - paginate/filter by name/status
  - Pagination: Start with simple offset pagination (`pageNumber`, `pageSize`) to reduce complexity for the 72h timeline. Cursor-based pagination can be added later if needed for scale.
- POST `/api/projects`
- GET `/api/projects/{id}` — include latest tasks summary
- PUT `/api/projects/{id}`
- DELETE `/api/projects/{id}` — soft delete
- GET `/api/projects/{id}/analytics` — counts by status, velocity basics

Tasks
- GET `/api/projects/{projectId}/tasks` - filter by status, sort; paginate (offset-based initially)
- POST `/api/projects/{projectId}/tasks`
- PUT `/api/tasks/{id}`
- PATCH `/api/tasks/{id}/status`
- DELETE `/api/tasks/{id}` - soft delete
- POST `/api/tasks/{id}/assign` - set `AssigneeUserId`
  - Alternative (more RESTful): `PATCH /api/tasks/{id}/assignee` with a minimal body `{ assigneeUserId }`. We keep the assessment’s route for compliance and may alias the PATCH.

Health & Monitoring
- GET `/health` — liveness/readiness incl. Mongo & Redis checks

Conventions
- Never expose domain entities; only DTOs from `Contracts`.
- Use ProblemDetails on errors; include correlation ID.
- Versioning via URL segment `v1` or header; initial is `v1`.
 - Concurrency (Phase 3): Support conditional updates using `ETag` and `If-Match` on write endpoints. `ETag` derives from `Id` + `UpdatedAt.Ticks` (or a server-maintained increment). Handlers include `UpdatedAt`/`Version` in Mongo filters to avoid lost updates. Initial release can omit this if time-constrained.

## Authentication Flow (Supabase)
Preferred (simpler) flow for timeline:
1) Client uses Supabase directly for register/login and obtains a JWT.
2) For protected endpoints, client sends `Authorization: Bearer <JWT>`.
3) API validates JWT via JWKS (issuer/audience) and populates `HttpContext.User`.

Alternative (if endpoints must exist):
- Provide thin pass-through endpoints `/api/auth/register|login` that forward to Supabase and return the payload. Keep logic minimal and secure (never log request bodies).

Security
- Enforce HTTPS behind reverse proxy; set `ForwardedHeaders`.
- CORS allowlist via env vars.
- Add security headers (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, etc.).
 - Never log secrets; skip request/response body logging for auth endpoints and scrub configured PII keys in logs (e.g., `email`, `password`).

## Application Layer (CQRS-lite)
- Use MediatR for commands/queries with minimal ceremony to signal modern .NET practices while keeping it lean.
- Commands return `Result`/`Result<T>`; queries return DTOs or `PagedResult<T>`.
- Validation via FluentValidation using a MediatR pipeline behavior.
- Add optional pipeline behaviors for logging/correlation (kept simple).
- Mapping via manual mapping or small helpers (YAGNI for heavy mappers).

## Repositories (Mongo)
- Interfaces: `IProjectRepository`, `ITaskRepository` (no generic repository anti‑pattern).
- Methods use explicit intent (e.g., `GetByOwnerAsync`, `GetByProjectAsync`, `UpdateStatusAsync`).
- Always apply `IsDeleted == false` unless explicitly overridden.

## Error Handling (Result Pattern)
- Business rule violations return typed errors (code, message), not exceptions.
- Exceptions reserved for truly exceptional infrastructure errors; logged by middleware.
- API returns HTTP ProblemDetails with consistent error codes.

## Caching Strategy (Redis)
- Read-through cache for hot reads:
  - Project list per user/page/filter
  - Task list per project/page/filter
- Key format: `v1:<tenant|user>:<entity>:<params hash>`
- Start simple: cache-aside with short TTLs (e.g., 30–60s) and direct key eviction for entity-by-id reads. Defer list caching until core flows stabilize.
- Advanced (optional): set-based invalidation to avoid SCANs; add when expanding list caching coverage.
- ETags: For GET by id, provide `ETag` (hash of last modified + doc id); support `If-None-Match`.

## Observability
- Serilog sinks: Console (JSON). Enrich with correlation ID, request ID, user ID.
- Request/response logging middleware with body size limits and PII scrubbing.
- HealthChecks UI compatible output; readiness checks ensure Mongo/Redis available.
 - Optional: OpenTelemetry tracing for API and Mongo client (bonus), exporting to console/OTLP when enabled.

## Resilience & Limits
- Rate limiting (ASP.NET Core built-in) per user/IP; token-bucket policy.
- Retry policies for outbound calls (Supabase) with Polly (exponential backoff).
- Circuit breaker for Supabase outages.
- Graceful shutdown hooks flush logs, stop background work.
 - Avoid Mongo multi-document transactions to keep dev/prod parity without replica sets. Prefer single-collection operations; accept eventual consistency for cross-collection effects (e.g., soft-deleted project hides its tasks via read filters). Handlers are idempotent to support retries.

## API Design Enhancements (Backlog)
- Versioning: `Microsoft.AspNetCore.Mvc.Versioning`
- ETags: as above for cache coherency.
- HATEOAS: defer to backlog; not required for initial delivery.
- Real-time updates (SignalR): backlog item; implement only if time permits.

## Docker & Deployment
- `docker-compose.yml` services:
  - `api`: build from `TaskService.Api/Dockerfile`, ports `5000:8080` (Kestrel on 8080)
  - `mongo`: with volume `mongo-data`, healthcheck, env `MONGO_INITDB_ROOT_*`
  - `redis`: default config, healthcheck
  - (optional) `nginx`: reverse proxy, gzip, security headers
- Profiles: `dev` and `prod` via compose overrides or env flags.
- Config through `.env` (see `.env.example`).

## Configuration (12‑factor)
Key variables (examples)
- `ASPNETCORE_ENVIRONMENT=Development`
- `MONGO__CONNECTION_STRING=`
- `MONGO__DATABASE=tasks`
- `REDIS__CONNECTION_STRING=`
- `SUPABASE__URL=`
- `SUPABASE__JWKS_URL=`
- `SUPABASE__SERVICE_KEY=` (server‑to‑server)
- `CORS__ALLOWED_ORIGINS=` (comma‑sep)
- `RATE_LIMIT__PER_MINUTE=60`

## Scalability Considerations
- Stateless API; cache externalized; DB horizontally scalable with sharding if needed (future).
- Indexes support hot paths; pagination uses `CreatedAt` + `_id` for stable ordering.
- Async I/O; bounded concurrency on heavy endpoints.
- Idempotent writes (e.g., `PATCH status`) to ease retries.

## Trade‑offs & Assumptions
- JWT validation relies on Supabase JWKS availability; we cache keys.
- No multi‑tenant data partitioning beyond `OwnerId` scoping (YAGNI).
- Text search optional; if used, add text index with caution (Atlas tier limits).
- Minimal HATEOAS to avoid over‑engineering; primary focus on clear, stable contracts.

## References
- ASP.NET Core Web API: https://learn.microsoft.com/aspnet/core/web-api
- MongoDB C# Driver: https://mongodb.github.io/mongo-csharp-driver/
- Serilog: https://serilog.net/
- Health Checks: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
- FluentValidation: https://docs.fluentvalidation.net/
- Rate Limiting: https://learn.microsoft.com/aspnet/core/performance/rate-limit
- Redis client: https://stackexchange.github.io/StackExchange.Redis/
- Supabase Auth (JWT): https://supabase.com/docs/guides/auth
 - (Optional) OpenTelemetry: https://opentelemetry.io/docs/instrumentation/net/
