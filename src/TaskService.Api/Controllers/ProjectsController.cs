using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskService.Application.Projects.Queries.GetProjects;
using TaskService.Application.Dtos.Common;
using TaskService.Application.Dtos.Projects;
using TaskService.Application.Projects.Commands;
using TaskService.Application.Projects.Queries;
using TaskService.Shared;
using Microsoft.AspNetCore.OutputCaching;
using TaskService.Api.Middleware;
using Microsoft.Net.Http.Headers;

namespace TaskService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id}")]
    [OutputCache(PolicyName = "Cache30s")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(result.Value!.ETag))
            {
                Response.Headers.ETag = result.Value!.ETag;
            }
            return Ok(result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpGet]
    [OutputCache(PolicyName = "CacheList30s")]
    [ProducesResponseType(typeof(PaginatedList<ProjectListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var ownerId = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value ?? string.Empty;
        var query = new GetProjectsQuery(ownerId, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value ?? string.Empty;
        var command = new CreateProjectCommand(ownerId, request.Name, request.Description);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var ifMatch = Request.Headers[HeaderNames.IfMatch].FirstOrDefault();
        var command = new UpdateProjectCommand(id, request.Name, request.Description)
            with { IfMatch = ifMatch };
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var ifMatch = Request.Headers[HeaderNames.IfMatch].FirstOrDefault();
        var command = new DeleteProjectCommand(id) { IfMatch = ifMatch };
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        var correlationId = HttpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value, correlationId);
        return StatusCode(problemDetails.Status, problemDetails);
    }
}
