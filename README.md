# TaskService

## Project Overview and Objectives

TaskService is a .NET 8 Web API for managing projects and tasks. It follows a clean architecture (API, Application, Domain, Infrastructure) and demonstrates production‑grade practices across authentication, validation, error handling, caching, observability, and scalability. Objectives:
- Provide a secure, versioned REST API for projects and tasks
- Implement robust validation, uniform error responses, and ETag-based optimistic concurrency
- Ensure performance via output caching and Redis caching with proper cache invalidation
- Offer strong observability: structured logging, request logging, correlation IDs, and health checks

## Tech Stack Justification
- .NET 8 Web API: modern, high‑performance, LTS platform with first‑class middleware, DI, and Http abstractions
- MediatR (CQRS): decouples concerns with pipeline behaviors for validation/logging
- MongoDB: flexible document storage, ideal for nested task data and rapid iteration; indexes provide performant queries
- Redis: fast, in‑memory caching for read‑heavy endpoints; simple cache invalidation patterns on writes
- Supabase Auth (JWT HS256): standards‑based auth with server‑validated tokens
- Serilog + HTTP logging: structured, machine‑parseable logs suitable for centralized collection

## Local Setup Instructions (step‑by‑step)
1) Prerequisites
   - .NET 8 SDK
   - Docker + Docker Compose

2) Configure Supabase (important)
   - In `src/TaskService.Api/appsettings.json`, set your project values under `Supabase`:
     - `Url = https://<ref>.supabase.co`
     - `Issuer = https://<ref>.supabase.co/auth/v1`
     - `Audience = authenticated`
     - Preferred: `JwtSecret = <Project JWT Secret>` (HS256)
     - Optionally: `ServiceKey` (for server‑side register/login calls)

3) Restore + Build
   - `dotnet restore`
   - `dotnet build`

4) Run API locally (without Docker)
   - `dotnet run --project src/TaskService.Api`
   - Browse Swagger (Development): `http://localhost:5214/swagger`

5) Headers to use
   - Version: `x-api-version: 1.0`
   - Authorization: `Bearer <accessToken>`
   - Optional correlation: `X-Correlation-ID: <uuid>`

## Docker Setup Commands
- Build and start services:
  - `docker compose up -d --build`
- Tail logs:
  - `docker compose logs -f api`
- Stop and remove containers:
  - `docker compose down`

## How to Run Tests
- Unit + Integration tests:
  - `dotnet test`
- Coverage (collector configured in tests):
  - `dotnet test --collect:"XPlat Code Coverage"`

## Environment Variables Explanation
- MongoDB
  - `MongoDB__ConnectionString` – Mongo connection string (e.g., `mongodb://mongo:27017`)
  - `MongoDB__DatabaseName` – Database name (default `taskservice`)
  - `MongoDB__CreateIndexesOnStartup` – Ensure indexes on startup (true/false)

- Redis
  - `Redis__ConnectionString` – Redis connection string (e.g., `redis:6379`)
  - `Redis__DefaultExpirationMinutes` – Default cache TTL for application cache

- Supabase
  - `Supabase__Url` – Project base URL (e.g., `https://<ref>.supabase.co`)
  - `Supabase__Issuer` – Token issuer, typically `<Url>/auth/v1`
  - `Supabase__Audience` – Expected audience, default `authenticated`
  - `Supabase__JwtSecret` – Project JWT secret (HS256 signature validation)
  - `Supabase__ApiKey` / `Supabase__ServiceKey` – Keys for calling Supabase Auth endpoints (register/login)
  - `Supabase__SkipEmailConfirmation` – Whether signup skips email confirmation

- ASP.NET Core
  - `ASPNETCORE_ENVIRONMENT` – `Development` enables Swagger, detailed errors

## Known Limitations
- External connectivity required for Supabase register/login (Auth endpoints)
- Output caching can serve stale reads for the policy window (default 30s)
- Simple global rate limit (fixed window) – tune per endpoint if needed
- ETag uses `UpdatedAt`; clock skew across instances should be minimal

---
