using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskService.Application.Projects.Queries.GetProjects;
using TaskService.Application.Dtos.Common;
using TaskService.Application.Dtos.Projects;
using TaskService.Application.Projects.Commands;
using TaskService.Application.Projects.Queries;
using TaskService.Shared;

namespace TaskService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/projects")]
[Authorize]
public sealed class ProjectsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var query = new GetProjectByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpGet]
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

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
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

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProjectCommand(id, request.Name, request.Description);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var command = new DeleteProjectCommand(id);
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }
}
