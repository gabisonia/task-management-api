using Serilog;
using Serilog.Formatting.Compact;
using TaskService.Api.Middleware;
using TaskService.Application;
using TaskService.Infrastructure;
using Microsoft.AspNetCore.HttpLogging;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskService API");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new Microsoft.AspNetCore.Mvc.Versioning.HeaderApiVersionReader("x-api-version");
    });

    builder.Services.AddVersionedApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = false;
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

        options.OperationFilter<TaskService.Api.Swagger.ApiVersionHeaderOperationFilter>();
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddMongoDb(
            _ => new MongoDB.Driver.MongoClient(builder.Configuration.GetSection("MongoDB:ConnectionString").Value ??
                                                "mongodb://localhost:27017"),
            name: "mongodb",
            timeout: TimeSpan.FromSeconds(3),
            tags: ["ready"])
        .AddRedis(
            builder.Configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379",
            name: "redis",
            timeout: TimeSpan.FromSeconds(3),
            tags: ["ready"]);

    // HTTP logging (headers/body sizes, exclude sensitive headers)
    builder.Services.AddHttpLogging(options =>
    {
        options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                                HttpLoggingFields.ResponsePropertiesAndHeaders;
        options.RequestHeaders.Add("x-api-version");
        options.ResponseHeaders.Add("x-correlation-id");
    });

    // Response compression for JSON
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.MimeTypes =
        [
            "application/json",
            "application/problem+json",
            "text/plain"
        ];
    });

    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("Cache30s", b => b
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByHeader("x-api-version")
            .SetVaryByRouteValue("id"));

        options.AddPolicy("CacheList30s", b => b
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByHeader("x-api-version")
            .SetVaryByQuery("pageNumber", "pageSize", "status", "projectId"));
    });

    // Basic IP-based rate limiting (100 req/min)
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            string key = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(key,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true
                });
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    WebApplication app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHttpLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseResponseCompression();

    app.UseMiddleware<SecurityHeadersMiddleware>();

    app.UseRateLimiter();
    app.UseOutputCache();
    app.UseAuthentication();
    app.UseAuthorization();

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
