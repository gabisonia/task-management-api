using FluentValidation;
using MediatR;
using TaskService.Domain.TaskItemManagement;
using TaskService.Shared;

namespace TaskService.Application.Tasks.Commands.DeleteTask;

public sealed record DeleteTaskCommand(string Id) : IRequest<Result>;

public sealed class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteTaskCommandHandler(ITaskRepository taskRepository)
    : IRequestHandler<DeleteTaskCommand, Result>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.Id, cancellationToken);
        if (task == null)
        {
            return Result.Failure(new Error("TASK_NOT_FOUND", $"Task with ID '{request.Id}' not found."));
        }

        await taskRepository.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
