using System.Net;
using System.Text.Json;
using TaskService.Shared;

namespace TaskService.Api.Middleware;

public sealed class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var correlationId = context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = new ProblemDetailsResponse
        {
            Status = context.Response.StatusCode,
            Title = "Internal Server Error",
            Detail = environment.IsDevelopment()
                ? $"{exception.Message}\n\nStack Trace:\n{exception.StackTrace}"
                : "An unexpected error occurred. Please try again later.",
            Type = $"https://httpstatuses.io/{context.Response.StatusCode}",
            Instance = context.Request.Path,
            ErrorCode = "INTERNAL_SERVER_ERROR",
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId
        };

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
