using FluentValidation;
using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Domain.TaskItemManagement;
using TaskService.Shared;

namespace TaskService.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(string Id) : IRequest<Result>
{
    public string? IfMatch { get; init; }
}

public sealed class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteTaskCommandHandler(ITaskRepository taskRepository, ICacheService cache)
    : IRequestHandler<DeleteTaskCommand, Result>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.Id, cancellationToken);
        if (task == null)
        {
            return Result.Failure(new Error("TASK_NOT_FOUND", $"Task with ID '{request.Id}' not found."));
        }

        // Concurrency check via ETag (If-Match)
        var currentEtag = ETagGenerator.From(task.Id.ToString(), task.UpdatedAt);
        if (!string.IsNullOrWhiteSpace(request.IfMatch) && !string.Equals(request.IfMatch, currentEtag, StringComparison.Ordinal))
        {
            return Result.Failure(
                new Error("PRECONDITION_FAILED", "The resource has been modified. ETag mismatch."));
        }

        await taskRepository.DeleteAsync(request.Id, cancellationToken);

        // Invalidate caches
        await cache.RemoveAsync($"tasks:{task.Id}", cancellationToken);
        await cache.RemoveByPatternAsync($"tasks:project:{task.ProjectId}:*", cancellationToken);
        return Result.Success();
    }
}
