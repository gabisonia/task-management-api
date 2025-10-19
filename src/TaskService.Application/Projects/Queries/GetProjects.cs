using MediatR;
using TaskService.Application.Abstractions;
using TaskService.Application.Dtos.Common;
using TaskService.Application.Dtos.Projects;
using TaskService.Domain.ProjectManagement;
using TaskService.Shared;

namespace TaskService.Application.Projects.Queries.GetProjects;

public sealed record GetProjectsQuery(string OwnerId, int PageNumber, int PageSize)
    : IRequest<Result<PaginatedList<ProjectListItemResponse>>>;

public sealed class GetProjectsQueryHandler(IProjectRepository projectRepository, ICacheService cache)
    : IRequestHandler<GetProjectsQuery, Result<PaginatedList<ProjectListItemResponse>>>
{
    public async Task<Result<PaginatedList<ProjectListItemResponse>>> Handle(GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        string cacheKey = $"projects:list:{request.OwnerId}:{request.PageNumber}:{request.PageSize}";
        var cached = await cache.GetAsync<PaginatedList<ProjectListItemResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<PaginatedList<ProjectListItemResponse>>.Success(cached);
        }

        int skip = (request.PageNumber - 1) * request.PageSize;
        (IReadOnlyList<Project> projects, long totalCount) = await projectRepository.GetByOwnerAsync(
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

        await cache.SetAsync(cacheKey, paginatedList, TimeSpan.FromMinutes(2), cancellationToken);
        return Result<PaginatedList<ProjectListItemResponse>>.Success(paginatedList);
    }
}
