using System.Net;

namespace TaskService.Shared;

public static class ProblemDetailsFactory
{
    public static ProblemDetailsResponse FromError(Error error, string? instance = null, string? correlationId = null)
    {
        (int statusCode, string title) = MapErrorToHttpStatus(error.Code);

        return new ProblemDetailsResponse
        {
            Status = statusCode,
            Title = title,
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = instance,
            ErrorCode = error.Code,
            CorrelationId = correlationId
        };
    }

    public static ProblemDetailsResponse FromResult(Result result, string? instance = null,
        string? correlationId = null)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot create ProblemDetails from a successful result.");
        }

        if (result.Error == null)
        {
            throw new InvalidOperationException("Failed result must have an error.");
        }

        return FromError(result.Error, instance, correlationId);
    }

    public static ProblemDetailsResponse FromValidationErrors(
        IDictionary<string, string[]> validationErrors,
        string? instance = null,
        string? correlationId = null)
    {
        return new ProblemDetailsResponse
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.io/400",
            Instance = instance,
            ErrorCode = "VALIDATION_FAILED",
            CorrelationId = correlationId,
            Extensions = new Dictionary<string, object> { ["errors"] = validationErrors }
        };
    }

    private static (int StatusCode, string Title) MapErrorToHttpStatus(string errorCode)
    {
        return errorCode switch
        {
            // Not Found (404)
            var code when code.Contains("NOT_FOUND") || code.Contains("NOTFOUND")
                => ((int)HttpStatusCode.NotFound, "Not Found"),

            // Validation/Bad Request (400)
            var code when code.Contains("VALIDATION") || code.Contains("INVALID")
                => ((int)HttpStatusCode.BadRequest, "Bad Request"),

            // Conflict (409)
            var code when code.Contains("CONFLICT") || code.Contains("DUPLICATE")
                => ((int)HttpStatusCode.Conflict, "Conflict"),

            // Precondition Failed (412)
            var code when code.Contains("PRECONDITION") || code.Contains("CONCURRENCY")
                => ((int)HttpStatusCode.PreconditionFailed, "Precondition Failed"),

            // Unauthorized (401)
            var code when code.Contains("UNAUTHORIZED") || code.Contains("UNAUTHENTICATED")
                => ((int)HttpStatusCode.Unauthorized, "Unauthorized"),

            // Forbidden (403)
            var code when code.Contains("FORBIDDEN") || code.Contains("ACCESS_DENIED")
                => ((int)HttpStatusCode.Forbidden, "Forbidden"),

            // Unprocessable Entity (422)
            var code when code.Contains("UNPROCESSABLE")
                => ((int)HttpStatusCode.UnprocessableEntity, "Unprocessable Entity"),

            // Internal Server Error (500) - default
            _ => ((int)HttpStatusCode.InternalServerError, "Internal Server Error")
        };
    }
}
