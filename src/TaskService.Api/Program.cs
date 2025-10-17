using Serilog;
using Serilog.Formatting.Compact;
using TaskService.Api.Middleware;
using TaskService.Application;
using TaskService.Infrastructure;

// Bootstrap logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskService API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    // Add services to the container
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = Microsoft.AspNetCore.Mvc.Versioning.ApiVersionReader.Combine(
            new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader(),
            new Microsoft.AspNetCore.Mvc.Versioning.HeaderApiVersionReader("x-api-version"),
            new Microsoft.AspNetCore.Mvc.Versioning.QueryStringApiVersionReader("api-version"));
    });

    builder.Services.AddVersionedApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1",
            new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Task Service API",
                Version = "v1",
                Description = "RESTful API for project and task management with Supabase authentication",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact { Name = "Task Service Team" }
            });

        // JWT Bearer authentication
        options.AddSecurityDefinition("Bearer",
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

        options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddMongoDb(
            _ => new MongoDB.Driver.MongoClient(builder.Configuration.GetSection("MongoDB:ConnectionString").Value ??
                                                "mongodb://localhost:27017"),
            name: "mongodb",
            timeout: TimeSpan.FromSeconds(3))
        .AddRedis(
            builder.Configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379",
            name: "redis",
            timeout: TimeSpan.FromSeconds(3));

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
    app.MapHealthChecks("/health/live",
        new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });

    app.MapControllers();

    Log.Information("TaskService API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program for WebApplicationFactory in tests
public partial class Program
{
}
