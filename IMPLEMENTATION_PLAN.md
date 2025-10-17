# Implementation Plan — Distributed Task Management API

Status legend: [ ] Not started · [~] In progress · [x] Done

## Epic 0 - Foundations, TDD, Git & MediatR
- [ ] Initialize solution and projects (`TaskService.sln`)
- [ ] Add projects: Api, Application, Domain, Infrastructure, Contracts, Shared
- [ ] Wire DI and conventional registrations
- [ ] Add analyzers and code style (nullable enabled, warnings as errors in CI)
- [ ] Add Serilog bootstrap logging
- [ ] Enable XML docs (`GenerateDocumentationFile`) in projects
- [ ] Add DocFX skeleton (optional if time) and CI job placeholder
- [ ] Git workflow: feature branches (`feat/<task>`), conventional commits
- [ ] TDD cycle template per task: write failing test → implement → refactor → docs
- [ ] Add MediatR packages and register pipeline behaviors (validation, optional logging)

## Epic 1 - Domain & Shared Kernel
- [ ] Define enums: `ProjectStatus`, `TaskStatus`, `Priority`
- [ ] Define entities: `Project`, `Task`
- [ ] Add `IHasAudit` interface, audit stamps
- [ ] Implement `Result`/`Result<T>` and error catalog
- [ ] Add `IDateTimeProvider`
 - [ ] Define concurrency mechanism (use `UpdatedAt` as token or add `Version`)

## Epic 2 - Contracts & Validation
- [ ] Define DTOs (v1) for Auth, Projects, Tasks, Paging
- [ ] Add FluentValidation validators for all request DTOs
- [ ] Add API versioning (v1) and ProblemDetails mapper
 - [ ] Standardize ETag/If-Match usage for PUT/PATCH endpoints

## Epic 3 - Infrastructure: MongoDB & Redis
- [ ] Mongo context and options binding
- [ ] Create collections and indexes (migrations initializer)
  - [ ] (Optional) Partial unique index: `projects(OwnerId+Name)` where `IsDeleted=false`
- [ ] Implement `IProjectRepository`
- [ ] Implement `ITaskRepository`
- [ ] Add Redis cache service (keys, TTL, invalidation sets)
 - [ ] Index-based pagination helpers (encode/decode continuation token)

## Epic 4 - Auth Integration (Supabase)
- [ ] JWT validation via JWKS (issuer/audience) middleware
- [ ] Prefer client-side Supabase auth; server validates JWT only
- [ ] (Optional) Thin pass-through endpoints for register/login if required
- [ ] `GET /api/auth/me` from claims

## Epic 5 - Application (CQRS via MediatR)
- [ ] Commands: Create/Update/Delete Project; Create/Update/Delete Task; Assign; Patch Status
- [ ] Queries: Get Projects (offset pagination), Get Project by Id (+tasks summary), Get Tasks by Project (offset pagination)
- [ ] Handlers use repositories + caching abstractions; manual mapping
- [ ] Pipeline behaviors: validation (mandatory), logging/correlation (optional)
- [ ] Concurrency (Phase 3): ETag/If‑Match and 412/409 mapping

## Epic 6 - API Endpoints & Middleware
- [ ] Minimal APIs or Controllers (Controllers preferred for versioning clarity)
- [ ] Authentication/Authorization policies
- [ ] Request logging and correlation ID middleware
- [ ] Response compression
- [ ] Rate limiting policies (per user/IP)
- [ ] Security headers & CORS
 - [ ] PII scrub & skip auth bodies in logging

## Epic 7 - Caching Strategy (phased)
- [ ] Phase A: Cache-by-id with TTL; evict on writes
- [ ] Phase B: Add list caching for hot lists (optional)
- [ ] Compute and set ETags for GET by Id (Phase 3)
- [ ] Advanced: set-based invalidation for lists (optional)

## Epic 8 - Observability & Health
- [ ] Serilog configuration (JSON console) with enrichers (UserId, CorrelationId)
- [ ] Health checks (self, Mongo, Redis)
- [ ] Slow query logging thresholds
 - [ ] Optional OpenTelemetry tracing (bonus)

## Epic 9 — Advanced Features (≥ 3)
- [ ] Redis caching with invalidation (Epic 7)
- [ ] Rate limiting (Epic 6)
- [ ] Structured + request/response logging (Epic 6/8)
- [ ] Indexing strategy (Epic 3)
- [ ] Optional: SignalR hub for task updates (project channels)

## Epic 10 - Docker & Environment
- [ ] API `Dockerfile` (multi-stage build)
- [ ] `docker-compose.yml` (api, mongo, redis [+ nginx optional])
- [ ] `.env.example` with all configuration keys
- [ ] Healthchecks in compose
 - [ ] Note: No Mongo replica set; avoid multi-document transactions
 - [ ] Secrets policy: Supabase service key only in server, never logged

## Epic 11 - Testing
- [ ] Unit tests: domain rules (statuses, transitions), validators (write first)
- [ ] Integration tests: repositories (with test Mongo), API endpoints (WebApplicationFactory) (write first for vertical slice)
- [ ] Test fixtures and data builders
- [ ] Smoke tests for docker-compose (basic liveness)
- [ ] Concurrency tests (ETag/If-Match, 412 Precondition Failed)

## Epic 12 — Postman Collection
- [ ] Environments (local/prod)
- [ ] Pre‑request auth scripts (Supabase token)
- [ ] Collections for all endpoints with examples
- [ ] Tests for key flows (create project -> create task -> update status)

## Epic 13 - CI/CD (Bonus)
- [ ] GitHub Actions: build, test, lint
- [ ] Docker build and compose up on PR (service health)
 - [ ] (Optional) DocFX docs build job

## Epic 14 — Documentation
- [ ] README (overview, setup, running, testing)
- [ ] ARCHITECTURE.md (completed)
- [ ] Update changelog/decisions (ADR‑style notes)

---

## Sequence & Milestones
Phase 1 (0–8h): Foundation & First Vertical Slice
- feat/foundation: Epics 0–3 minimal; Project entity end‑to‑end with tests and Docker

Phase 2 (8–24h): Core Features
- feat/core-api: Projects CRUD (TDD); Tasks CRUD (TDD); JWT validation only; basic health; compose works

Phase 3 (24–48h): Hardening
- feat/advanced: Redis cache (Phase A), rate limiting, structured logging, input validation; integration tests; add ETag/If‑Match concurrency and 412 handling

Phase 4 (48–60h): Polish
- feat/docs-and-testing: Postman collection, doc updates (README, ARCHITECTURE), ensure compose up is flawless

Phase 5 (60–72h): Buffer & Deployment
- Deployment to free tier; smoke tests; optional one bonus feature

## Definition of Done (per epic)
- Compiles and passes tests
- Coding standards satisfied (analyzers) and reviewed
- Logging, errors, and validation verified
- Documentation updated
