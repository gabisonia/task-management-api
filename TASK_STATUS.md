# Task Status - Distributed Task Management API

Legend: [ ] Not started · [~] In progress · [x] Done

Overall Progress: 29% (Epics 0-3 complete)

## Epic 0 — Foundations, TDD & Git ✅
- [x] Solution and projects scaffolded
- [x] Analyzers/coding style configured (Directory.Build.props with EnableNETAnalyzers)
- [x] Serilog bootstrap logging
- [x] XML docs enabled
- [x] Git workflow (conventional commits, working on master for assessment)
- [x] Result pattern implemented
- [x] Build verified (0 warnings)

**Status:** COMPLETE ✅  
**Completed:** 2025-10-17

## Epic 1 — Domain & Shared ✅
- [x] Enums defined (ProjectStatus, TaskStatus, Priority)
- [x] Entities defined (Project, TaskItem with MongoDB attributes)
- [x] IHasAudit interface implemented
- [x] Result/Errors implemented (error catalogs created)
- [x] DateTime provider (IDateTimeProvider abstraction)
- [x] Concurrency token decision (UpdatedAt; implementation planned for Phase 3)
- [x] Unit tests (10 tests, all passing)

**Status:** COMPLETE ✅  
**Completed:** 2025-10-17

## Epic 2 - Contracts & Validation ✅
- [x] Request/Response DTOs (v1)
- [x] FluentValidation validators
- [x] ProblemDetails mapper
- [x] ETag/If-Match contract defined

**Status:** COMPLETE ✅  
**Completed:** 2025-10-17

## Epic 3 - Persistence ✅
- [x] Mongo options & context
- [x] Index initializer
- [x] ProjectRepository
- [x] TaskRepository
- [x] Redis Cache service
- [x] Partial unique index (OwnerId+Name, IsDeleted=false)
- [x] DateTimeProvider implementation

**Status:** COMPLETE ✅  
**Completed:** 2025-10-17  
**Note:** Integration tests require MongoDB/Redis running (Epic 11 will add testcontainers)

## Epic 4 - Auth (Supabase)
- [ ] JWT validation via JWKS
- [ ] Register/Login proxy
- [ ] `GET /api/auth/me`

## Epic 5 - Application (MediatR CQRS‑lite)
- [ ] MediatR packages and DI
- [ ] Validation pipeline behavior
- [ ] Handlers (projects)
- [ ] Handlers (tasks)
- [ ] Queries (projects/tasks)
- [ ] Concurrency (Phase 3) ETag/If‑Match

## Epic 6 - API & Middleware
- [ ] Controllers + versioning
- [ ] Correlation & request logging
- [ ] Response compression
- [ ] Rate limiting
- [ ] CORS & security headers
 - [ ] PII scrub & auth body skip

## Epic 7 - Caching (phased)
- [ ] Phase A: by‑id cache + TTL + evict
- [ ] Phase B: list cache (optional)
- [ ] ETags for GET by Id
- [ ] Advanced: set‑based invalidation for lists (optional)

## Epic 8 - Observability & Health
- [ ] Serilog enrichers configured
- [ ] Health checks (self, Mongo, Redis)
- [ ] Slow query logging
 - [ ] Optional OpenTelemetry

## Epic 9 - Advanced
- [ ] Redis caching complete
- [ ] Rate limiting complete
- [ ] Structured + req/resp logging
- [ ] Index strategy documented
- [ ] (Optional) SignalR task updates

## Epic 10 - Docker & Env
- [ ] API Dockerfile
- [ ] docker-compose with healthchecks
- [ ] .env.example
 - [ ] Note on transactions/replica set
 - [ ] Secrets policy for Supabase key

## Epic 11 - Testing
- [ ] Unit tests (domain, validators)
- [ ] Integration tests (repos, API)
- [ ] Smoke tests (compose)
 - [ ] Concurrency precondition tests

## Epic 12 - Postman
- [ ] Environments (local/prod)
- [ ] Pre‑request auth scripts
- [ ] Full collection + examples
- [ ] Tests for key flows

## Epic 13 - CI/CD (Bonus)
- [ ] GH Actions build/test
- [ ] Docker build

## Epic 14 - Documentation
- [x] ARCHITECTURE.md drafted
- [x] IMPLEMENTATION_PLAN.md drafted
- [x] README
