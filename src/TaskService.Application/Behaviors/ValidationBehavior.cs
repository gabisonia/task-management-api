#pragma warning disable CA2016 // CancellationToken is propagated by MediatR pipeline automatically

using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TaskService.Shared;

namespace TaskService.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result, new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count > 0)
        {
            string errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var error = new Error("VALIDATION_FAILED", errorMessage);

            Type resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                MethodInfo? failureMethod = resultType.GetMethod(nameof(Result<object>.Failure));
                if (failureMethod != null)
                {
                    return (TResponse)failureMethod.Invoke(null, [error])!;
                }
            }

            return (TResponse)Result.Failure(error);
        }

        return await next(cancellationToken);
    }
}
