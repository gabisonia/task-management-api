# Distributed Task Management API

A production-ready, cloud-native Task Management REST API built with .NET 8, MongoDB, Redis, and Supabase authentication. Implements clean architecture, CQRS with MediatR, and comprehensive testing.

## 📊 Current Status

**🎉 All EPICs Complete - Production Ready!** ✅

- **Epic 1**: Domain Models & Shared Kernel ✅
- **Epic 2**: Contracts & Validation ✅
- **Epic 3**: Persistence (MongoDB & Redis) ✅
- **Epic 4**: Authentication (Supabase JWT) ✅
- **Epic 5**: CQRS with MediatR ✅
- **Epic 6**: API Controllers ✅
- **Epic 7**: Global Exception Handling ✅
- **Epic 8**: API Documentation (Swagger) ✅
- **Epic 9**: Health Checks ✅
- **Epic 10**: Docker & Deployment ✅
- **Epic 11**: Testing (38 unit tests) ✅
- **Progress**: 100% - All epics complete

## 🏗️ Architecture

- **Clean Architecture** with CQRS pattern (MediatR)
- **Domain-Driven Design** with rich domain models
- **Repository Pattern** for data access abstraction
- **Result Pattern** for error handling (no exceptions for business logic)
- **Vertical Slice Architecture** for feature organization

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed design decisions.

## 🛠️ Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 8.0 |
| **Language** | C# | 12 |
| **Database** | MongoDB | 3.5.0 |
| **Cache** | Redis | 2.9.32 |
| **Auth** | Supabase JWT | - |
| **CQRS** | MediatR | 13.0.0 |
| **Validation** | FluentValidation | 12.0.0 |
| **Logging** | Serilog | 9.0.0 |
| **Testing** | xUnit + FluentAssertions | - |
| **Containers** | Docker + Docker Compose | - |

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.100 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for MongoDB and Redis)
- IDE: Visual Studio 2022, Rider, or VS Code

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Assessment
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Configure Supabase (required for authentication)**
   Update `src/TaskService.Api/appsettings.json` with your Supabase credentials:
   ```json
   "Supabase": {
     "Url": "https://your-project.supabase.co",
     "JwksUrl": "https://your-project.supabase.co/auth/v1/.well-known/jwks.json",
     "Issuer": "https://your-project.supabase.co/auth/v1",
     "Audience": "authenticated",
     "ServiceKey": "your-service-role-key"
   }
   ```

6. **Start the complete application stack with Docker**
   ```bash
   docker-compose up -d
   ```
   This starts:
   - MongoDB (port 27017)
   - Redis (port 6379)
   - Task Service API (ports 5000/5001)

7. **Alternative: Run API locally with Docker dependencies**
   ```bash
   # Start only MongoDB and Redis
   docker-compose up -d mongodb redis
   
   # Run API locally
   dotnet run --project src/TaskService.Api
   ```

8. **Access the API**
   - Swagger UI: http://localhost:5000/swagger
   - Health checks:
     - All: http://localhost:5000/health
     - Liveness: http://localhost:5000/health/live
     - Readiness: http://localhost:5000/health/ready
   - API endpoints: http://localhost:5000/api/v1/

## 📁 Solution Structure

```
├── src/
│   ├── TaskService.Api/            # REST API endpoints, middleware, DI configuration
│   ├── TaskService.Application/    # CQRS handlers, validators, DTOs
│   ├── TaskService.Domain/         # Entities, enums, domain logic
│   ├── TaskService.Infrastructure/ # MongoDB, Redis, external services
│   ├── TaskService.Application/Dtos/ # Public API contracts (request/response DTOs)
│   └── TaskService.Shared/         # Cross-cutting concerns (Result, Error, etc.)
├── tests/
│   ├── TaskService.UnitTests/      # Domain & application logic tests
│   └── TaskService.IntegrationTests/ # API & infrastructure tests
├── ARCHITECTURE.md                  # Architectural decisions
├── IMPLEMENTATION_PLAN.md           # Epic breakdown
└── TASK_STATUS.md                   # Progress tracking
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/TaskService.UnitTests
dotnet test tests/TaskService.IntegrationTests
```

**Current Test Coverage:**
- Unit Tests: 38 tests, 100% passing
  - Domain entity validation
  - FluentValidation validators (Projects, Tasks, Auth)
  - All critical business logic covered
- Integration Tests: 1 smoke test (requires MongoDB running)

**Note:** Integration tests require MongoDB and Redis to be running:
```bash
docker-compose up -d mongodb redis
dotnet test
```

## 🔧 Development

### Code Quality

- **.NET Analyzers** enabled (latest-recommended)
- **StyleCop.Analyzers** for code style consistency
- **Nullable reference types** enabled
- **Warnings as errors** in all projects
- **XML documentation** required for public APIs

### Validation Strategy

- **FluentValidation** for DTO validation
- **MediatR Pipeline Behavior** for automatic validation before command/query execution
- Validators defined in `TaskService.Application/Dtos` alongside DTOs
- Validation errors return `Result.Failure` with detailed error messages
- No ASP.NET Core auto-validation (validation handled in application layer)

### Build Configuration

Common project settings are centralized in `Directory.Build.props`:
- Target Framework: .NET 8
- Language Version: C# 12
- Analyzers: Enabled with consistent rules
- Documentation: XML comments required

### Commit Conventions

Following [Conventional Commits](https://www.conventionalcommits.org/):
- `feat(scope): description` - New features
- `fix(scope): description` - Bug fixes
- `docs(scope): description` - Documentation
- `chore(scope): description` - Build/tooling changes

## 🎯 Features

### Authentication & Authorization
- ✅ Supabase JWT authentication with JWKS validation
- ✅ Bearer token support in Swagger UI
- ✅ Protected endpoints with `[Authorize]` attribute
- ✅ User claims extraction (sub, email, roles)

### Project Management
- ✅ Create, read, update, delete projects
- ✅ Project ownership per authenticated user
- ✅ Pagination support for project listings
- ✅ Soft delete with filtering
- ✅ Duplicate name validation per owner

### Task Management
- ✅ Create, read, update, delete tasks within projects
- ✅ Task assignment to users
- ✅ Status workflow (New, InProgress, Blocked, Done)
- ✅ Priority levels (Low, Medium, High, Urgent)
- ✅ Due date tracking
- ✅ Tag-based categorization
- ✅ Pagination and filtering by status

### Architecture Highlights
- ✅ CQRS pattern with MediatR (Commands & Queries)
- ✅ Validation pipeline with FluentValidation
- ✅ Logging pipeline with Serilog
- ✅ Global exception handling middleware
- ✅ MongoDB with indexes and soft deletes
- ✅ Redis caching service
- ✅ Health checks for dependencies
- ✅ API versioning (v1.0)
- ✅ OpenAPI/Swagger documentation

## 📄 License

This is an assessment project.

## 🤝 Contributing

This is a solo assessment project. No external contributions accepted.

---

**Built with ❤️ using .NET 8 and Clean Architecture principles**
