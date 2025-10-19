# TaskService Architecture

## System Architecture Diagram

```
----------------------+        ----------------------+        ----------------------
|      HTTP Client     | --->  |   TaskService API    | --->  |   Application Layer  |
|  (Postman/Frontend)  |       | (Controllers, MW)    |       | (MediatR CQRS, DTOs) |
----------------------+        ----------------------+        ----------------------
                                 |            ^                        |
                                 v            |                        v
                           ----------------------+        ----------------------
                           |     Domain Layer     | <-->  |  Infrastructure      |
                           | (Entities, Errors)   |       |  (Mongo, Redis, Auth) |
                           ----------------------+        ----------+-----------
                                                                        |
                                                                        v
                                                       -------------------------------
                                                       | External Services             |
                                                       | - MongoDB (persistence)       |
                                                       | - Redis (cache)               |
                                                       | - Supabase Auth (JWT HS256)   |
                                                       -------------------------------
```

- API Layer: Controllers, middleware (exception handling, security headers, correlation IDs), versioning, output caching, health checks.
- Application Layer: Commands/queries (MediatR), validation/logging behaviors, DTO mapping, cache usage/invalidation.
- Domain Layer: Entities and errors only.
- Infrastructure Layer: MongoDB repositories, Redis cache service, Supabase auth client, JWT validation wiring.

## Database Schema Design and Justification

Collections
- `projects`
  - Fields: `_id (ObjectId)`, `ownerId (string)`, `name (string)`, `description (string?)`, `status (enum)`, `createdAt`, `updatedAt`, `isDeleted (bool)`
  - Indexes (see src/TaskService.Infrastructure/Persistence/MongoDB/MongoDbIndexInitializer.cs:1):
    - `idx_owner_isdeleted_created`: `{ ownerId: 1, isDeleted: 1, createdAt: -1 }` – efficient owner lists
    - `idx_owner_name_unique` (partial unique on `{ ownerId: 1, name: 1 }` where `isDeleted=false`) – prevents duplicate active names per owner

- `tasks`
  - Fields: `_id (ObjectId)`, `projectId (ObjectId)`, `title`, `description?`, `status (enum)`, `priority (enum)`, `assigneeUserId?`, `dueDate?`, `tags[]`, `createdAt`, `updatedAt`, `isDeleted (bool)`
  - Indexes:
    - `idx_project_isdeleted_created`: `{ projectId: 1, isDeleted: 1, createdAt: -1 }` – project task lists
    - `idx_assignee_status`: `{ assigneeUserId: 1, status: 1 }` – assignee dashboards

Justification
- Soft deletes (`isDeleted`) retain history and enable partial unique constraints.
- Time‑based sorting index for pagination; targeted indexes for most frequent filters (owner/project/assignee).

## Design Patterns Used and Why
- Clean Architecture: isolation for testability and maintainability.
- CQRS with MediatR: clear separation of reads/writes and uniform cross‑cutting behaviors.
- Repository pattern: abstracts Mongo specifics from domain.
- Middleware: cross‑cutting concerns (errors, headers, correlation) at the edge.

## Caching Strategy
- Output Caching: short TTL policies on GET endpoints; vary by id/query for correctness.
- Redis Application Cache:
  - Read‑through for queries (projects/tasks by id and list variants)
  - Invalidation on create/update/delete using precise keys/patterns

## Authentication Flow
- Client calls Auth endpoints (register/login) – API forwards to Supabase using configured key.
- Supabase issues JWT (HS256). API validates via `JwtBearer` using Supabase Project JWT Secret.
- `/api/auth/me` reads claims (sub/email/role/user_metadata) and returns a user profile.

## Error Handling Approach
- Global exception middleware produces RFC 7807 ProblemDetails with correlation ID.
- Domain/application errors mapped to HTTP codes through `ProblemDetailsFactory`.
- Validation behavior aggregates FluentValidation errors.

## Scalability Considerations
- Stateless API; state in MongoDB/Redis allows horizontal scaling.
- MongoDB indexes for hot paths; partial unique index for correctness at minimal overhead.
- Output caching and Redis reduce pressure on Mongo.
- Fixed window rate limiting protects from bursts; tune as needed.
