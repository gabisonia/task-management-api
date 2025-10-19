using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TaskService.Application.Dtos.Auth;
using TaskService.Shared;
using TaskService.Api.Middleware;
using TaskService.Application.Auth.Commands;
using TaskService.Application.Auth.Queries;

namespace TaskService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RegisterUserCommand(request.Email, request.Password, request.DisplayName), cancellationToken);

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
        var result = await mediator.Send(new LoginUserCommand(request.Email, request.Password), cancellationToken);

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
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await mediator.Send(new GetCurrentUserQuery());
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }
}
