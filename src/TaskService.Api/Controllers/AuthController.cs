using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;
using TaskService.Api.Middleware;

namespace TaskService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]
public sealed class AuthController(ISupabaseAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        ProblemDetailsResponse problemDetails =
            Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        ProblemDetailsResponse problemDetails =
            Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value
                     ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("user_id")?.Value;
        var email = User.FindFirst("email")?.Value;
        var displayName = User.FindFirst("name")?.Value ?? User.FindFirst("display_name")?.Value;

        // Try to extract from user_metadata claim (JSON) if not directly present
        var userMetadataJson = User.FindFirst("user_metadata")?.Value;
        if (string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(userMetadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(userMetadataJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("display_name", out var dnProp) && dnProp.ValueKind == JsonValueKind.String)
                {
                    displayName = dnProp.GetString();
                }
                else if (root.TryGetProperty("full_name", out var fnProp) && fnProp.ValueKind == JsonValueKind.String)
                {
                    displayName = fnProp.GetString();
                }
                else if (root.TryGetProperty("name", out var nProp) && nProp.ValueKind == JsonValueKind.String)
                {
                    displayName = nProp.GetString();
                }
            }
            catch
            {
                // ignore invalid json
            }
        }

        bool emailVerified = User.FindFirst("email_verified")?.Value == "true";
        if (!emailVerified && !string.IsNullOrEmpty(userMetadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(userMetadataJson);
                if (doc.RootElement.TryGetProperty("email_verified", out var evProp) &&
                    evProp.ValueKind == JsonValueKind.True)
                {
                    emailVerified = true;
                }
            }
            catch { }
        }

        var role = User.FindFirst("role")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            ProblemDetailsResponse problemDetails = Shared.ProblemDetailsFactory.FromError(
                new Error("AUTH_INVALID_TOKEN", "User ID not found in token"),
                HttpContext.Request.Path.Value);
            return StatusCode(problemDetails.Status, problemDetails);
        }

        var userInfo = new UserInfoResponse
        {
            Id = userId,
            Email = email ?? string.Empty,
            DisplayName = displayName,
            EmailVerified = emailVerified,
            Roles = role != null ? new[] { role } : Array.Empty<string>()
        };

        return Ok(userInfo);
    }
}
