using MediatR;
using TaskService.Application.Dtos.Common;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery(string OwnerId, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<ProjectListItemResponse>>>;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, Result<PaginatedList<ProjectListItemResponse>>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<PaginatedList<ProjectListItemResponse>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        int skip = (request.PageNumber - 1) * request.PageSize;
        (IReadOnlyList<Project> projects, long totalCount) = await _projectRepository.GetByOwnerAsync(
            request.OwnerId,
            skip,
            request.PageSize,
            cancellationToken);

        var items = projects.Select(p => new ProjectListItemResponse
        {
            Id = p.Id.ToString(),
            Name = p.Name,
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        var paginatedList = new PaginatedList<ProjectListItemResponse>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = (int)totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };

        return Result<PaginatedList<ProjectListItemResponse>>.Success(paginatedList);
    }
}
