# AGENTS.md — Working in This Repository

Scope: This file applies to the entire repository.

## Purpose
This project implements a Distributed Task Management API in .NET 7 with MongoDB, Redis, and Supabase JWT. Follow these conventions to keep changes clean, testable, and easy to review.

## Engineering Principles
- SOLID, KISS, YAGNI; avoid god classes/methods
- Prefer composition over inheritance; keep functions small and pure where possible
- No domain entities in controllers; use DTOs from `TaskService.Application.Dtos`
- Validation at boundaries (FluentValidation); business rules return `Result` instead of throwing
- Infrastructure exceptions are logged; API responds with ProblemDetails

## Project Structure
See `ARCHITECTURE.md:1` for a full overview. High level:
- `TaskService.Api` - endpoints, DI composition, middleware
- `TaskService.Application` - MediatR handlers (CQRS-lite), validators, mapping
- `TaskService.Domain` — entities, enums, domain services
- `TaskService.Infrastructure` — Mongo/Redis, repositories, Supabase client
- `TaskService.Application/Dtos` — request/response models (versioned)
- `TaskService.Shared` — Result, errors, pagination, time
- `tests/*` — unit and integration tests

## Coding Conventions
- C# 10+ features, nullable enabled, async all the way
- DI via constructor injection only; avoid service locators
- Keep files focused: one public type per file (except small records)
- Names:
  - Commands/Queries: `<Verb><Entity>` (e.g., `CreateProjectCommand`)
  - DTOs: `<Entity><Action>Request/Response`
  - Repos: `I<Project|Task>Repository`
- Use `Result/Result<T>` for business flows; avoid exceptions in happy paths
 - Concurrency & Caching:
   - Set `ETag` on GET by id using `UpdatedAt` and validate `If-Match` on PUT/PATCH; map mismatches to 412.
   - Prefer cursor-based pagination with continuation tokens; support pageNumber/pageSize as fallback.
   - Use Redis set-based invalidation; avoid SCAN/pattern deletes.
 - Documentation:
   - Keep XML documentation comments up to date for public APIs
   - If DocFX is enabled, ensure docs build passes in CI and update docs with code changes

## Branching & Commits
- Keep PRs small and focused on one epic/task
- Use conventional commits, e.g., `feat(api): add create project endpoint`
- Always work on a feature branch: `feat/<task-name>`
- Run `dotnet build` and `dotnet test` locally before pushing
- Follow TDD: write a failing test, implement, refactor, then commit

## Running (once implemented)
- Local dev: `docker-compose up -d` (Mongo, Redis) then `dotnet run -p src/TaskService.Api`
- All in Docker: `docker-compose up --build`
- Health: `GET http://localhost:5000/health`

## Testing (once implemented)
- Unit tests: `dotnet test tests/TaskService.UnitTests`
- Integration: `dotnet test tests/TaskService.IntegrationTests`
 - Concurrency: tests should verify 412 on stale `If-Match`

## Updating Plans & Status
- Add tasks in `IMPLEMENTATION_PLAN.md:1` as scope evolves
- Track progress in `TASK_STATUS.md:1` using [ ], [~], [x]

## Useful References
- ASP.NET Core Web API: https://learn.microsoft.com/aspnet/core/web-api
- MongoDB C# Driver: https://mongodb.github.io/mongo-csharp-driver/
- Serilog: https://serilog.net/
- Health Checks: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
- FluentValidation: https://docs.fluentvalidation.net/
- Rate Limiting: https://learn.microsoft.com/aspnet/core/performance/rate-limit
- StackExchange.Redis: https://stackexchange.github.io/StackExchange.Redis/
- Supabase Auth: https://supabase.com/docs/guides/auth
- DocFX: https://dotnet.github.io/docfx/
 - MediatR: https://github.com/jbogard/MediatR

## Notes for Agents
- Do not introduce new cross-cutting layers without need (YAGNI)
- Keep logging structured; never log secrets
- Ensure endpoints never leak domain entities
- When adding a new endpoint, always add:
  1) DTOs and validators
  2) Command/Query + Handler (Application via MediatR)
  3) Repository method(s) (Infrastructure)
  4) Endpoint wiring (Api)
  5) Unit/Integration tests
  6) Postman request and docs
 - Security:
   - Do not log bodies of auth endpoints; scrub `email`, `password`, `Authorization` headers.
   - Supabase service key must only be used server-side; never expose to clients or logs.
