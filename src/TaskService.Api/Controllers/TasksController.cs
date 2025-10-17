using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskService.Application.Tasks.Commands.CreateTask;
using TaskService.Application.Tasks.Commands.DeleteTask;
using TaskService.Application.Tasks.Commands.UpdateTask;
using TaskService.Application.Dtos.Common;
using TaskService.Application.Dtos.Tasks;
using TaskService.Application.Tasks.Queries;
using TaskService.Shared;
using TaskService.Domain.TaskItemManagement;

namespace TaskService.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tasks")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var query = new GetTaskByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TaskListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] string projectId,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTasksQuery(projectId, status, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpPost("projects/{projectId}/tasks")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(string projectId, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Priority>(request.Priority, true, out var priority))
        {
            priority = Priority.Medium;
        }

        var command = new CreateTaskCommand(
            projectId,
            request.Title,
            request.Description,
            TaskItemStatus.New,
            priority,
            request.AssigneeUserId,
            request.DueDate,
            request.Tags ?? []);

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<TaskItemStatus>(request.Status, true, out var status))
        {
            status = TaskItemStatus.New;
        }

        if (!Enum.TryParse<Priority>(request.Priority, true, out var priority))
        {
            priority = Priority.Medium;
        }

        var command = new UpdateTaskCommand(
            id,
            request.Title,
            request.Description,
            status,
            priority,
            request.DueDate,
            request.Tags ?? []);

        var result = await _mediator.Send(command, cancellationToken);

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
        var command = new DeleteTaskCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        var problemDetails = Shared.ProblemDetailsFactory.FromError(result.Error!, HttpContext.Request.Path.Value);
        return StatusCode(problemDetails.Status, problemDetails);
    }
}
