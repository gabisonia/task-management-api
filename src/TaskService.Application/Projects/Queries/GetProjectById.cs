using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Queries;

public sealed record GetProjectByIdQuery(string Id) : IRequest<Result<ProjectResponse>>;

public sealed class GetProjectByIdQueryHandler(IProjectRepository projectRepository, ICacheService cache)
    : IRequestHandler<GetProjectByIdQuery, Result<ProjectResponse>>
{
    public async Task<Result<ProjectResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"projects:{request.Id}";
        var cached = await cache.GetAsync<ProjectResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<ProjectResponse>.Success(cached);
        }

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
            UpdatedAt = project.UpdatedAt,
            ETag = ETagGenerator.From(project.Id.ToString(), project.UpdatedAt)
        };

        await cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);
        return Result<ProjectResponse>.Success(response);
    }
}
