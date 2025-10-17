# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and props for better layer caching
COPY TaskService.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY global.json ./

# Copy project files
COPY src/TaskService.Api/TaskService.Api.csproj src/TaskService.Api/
COPY src/TaskService.Application/TaskService.Application.csproj src/TaskService.Application/
COPY src/TaskService.Infrastructure/TaskService.Infrastructure.csproj src/TaskService.Infrastructure/
COPY src/TaskService.Domain/TaskService.Domain.csproj src/TaskService.Domain/
COPY src/TaskService.Shared/TaskService.Shared.csproj src/TaskService.Shared/

# Restore
RUN dotnet restore TaskService.sln

# Copy the rest of the source
COPY src/ src/

# Publish API
RUN dotnet publish src/TaskService.Api/TaskService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Listen on 8080 inside container
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskService.Api.dll"]

