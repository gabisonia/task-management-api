using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;

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

        ProblemDetailsResponse problemDetails =
            Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
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

        ProblemDetailsResponse problemDetails =
            Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        var email = User.FindFirst("email")?.Value;
        var displayName = User.FindFirst("name")?.Value ?? User.FindFirst("display_name")?.Value;
        var emailVerified = User.FindFirst("email_verified")?.Value == "true";
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
