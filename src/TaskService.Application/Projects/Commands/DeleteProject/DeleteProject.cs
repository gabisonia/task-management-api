using FluentValidation;
using MediatR;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Commands.DeleteProject;

public sealed record DeleteProjectCommand(string Id) : IRequest<Result>;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result>
{
    private readonly IProjectRepository _projectRepository;

    public DeleteProjectCommandHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        Project? project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        if (project == null)
        {
            return Result.Failure(new Error("PROJECT_NOT_FOUND", $"Project with ID '{request.Id}' not found."));
        }

        await _projectRepository.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
