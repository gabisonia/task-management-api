#pragma warning disable CA2016 // CancellationToken is propagated by MediatR pipeline automatically

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskService.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Handling {RequestName}",
            requestName);

        var stopwatch = Stopwatch.StartNew();

        TResponse response = await next();

        stopwatch.Stop();

        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMilliseconds}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
