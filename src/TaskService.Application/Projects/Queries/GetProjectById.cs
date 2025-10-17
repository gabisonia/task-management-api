using MediatR;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Queries;

public sealed record GetProjectByIdQuery(string Id) : IRequest<Result<ProjectResponse>>;

public sealed class GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    : IRequestHandler<GetProjectByIdQuery, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetByIdAsync(request.Id, cancellationToken);

        if (project == null)
        {
            return Result<ProjectResponse>.Failure(
                new Error("PROJECT_NOT_FOUND", $"Project with ID '{request.Id}' not found."));
        }

        var response = new ProjectResponse
        {
            Id = project.Id.ToString(),
            OwnerId = project.OwnerId,
            Name = project.Name,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return Result<ProjectResponse>.Success(response);
    }
}
