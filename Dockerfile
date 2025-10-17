# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY Directory.Build.props .
COPY src/TaskService.Api/TaskService.Api.csproj src/TaskService.Api/
COPY src/TaskService.Application/TaskService.Application.csproj src/TaskService.Application/
COPY src/TaskService.Infrastructure/TaskService.Infrastructure.csproj src/TaskService.Infrastructure/
COPY src/TaskService.Domain/TaskService.Domain.csproj src/TaskService.Domain/
# Contracts project removed; DTOs live in Application/Dtos
COPY src/TaskService.Shared/TaskService.Shared.csproj src/TaskService.Shared/

RUN dotnet restore src/TaskService.Api/TaskService.Api.csproj

# Copy all source files and build
COPY src/. src/
WORKDIR /src/src/TaskService.Api
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

# Create non-root user
RUN useradd -m -u 1000 taskservice && chown -R taskservice:taskservice /app
USER taskservice

ENTRYPOINT ["dotnet", "TaskService.Api.dll"]
