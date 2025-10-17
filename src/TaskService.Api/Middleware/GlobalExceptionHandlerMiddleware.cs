using System.Net;
using System.Text.Json;
using TaskService.Shared;

namespace TaskService.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var problemDetails = new ProblemDetailsResponse
        {
            Status = context.Response.StatusCode,
            Title = "Internal Server Error",
            Detail = _environment.IsDevelopment() 
                ? $"{exception.Message}\n\nStack Trace:\n{exception.StackTrace}" 
                : "An unexpected error occurred. Please try again later.",
            Type = $"https://httpstatuses.io/{context.Response.StatusCode}",
            Instance = context.Request.Path,
            ErrorCode = "INTERNAL_SERVER_ERROR"
        };

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
