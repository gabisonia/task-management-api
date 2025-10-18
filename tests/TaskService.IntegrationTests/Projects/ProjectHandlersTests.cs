using TaskService.Application.Projects.Commands;
using TaskService.Application.Projects.Queries;
using TaskService.Application.Projects.Queries.GetProjects;
using TaskService.IntegrationTests.Fixtures;
using TaskService.Shared;

namespace TaskService.IntegrationTests.Projects;

public sealed class ProjectHandlersTests : IClassFixture<TestcontainersFixture>
{
    private readonly TestcontainersFixture _fx;

    public ProjectHandlersTests(TestcontainersFixture fx) => _fx = fx;

    [Fact]
    public async Task Create_Then_GetById_Succeeds()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var command = new CreateProjectCommand("owner-1", "Alpha", "First project");

        // Act
        var createResult = await mediator.Send(command);
        var getResult = await mediator.Send(new GetProjectByIdQuery(createResult.Value!.Id));

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value!.Name.Should().Be("Alpha");
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.Id.Should().Be(createResult.Value.Id);
        getResult.Value!.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task Update_Detects_Duplicate_Name()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var existing1 = await mediator.Send(new CreateProjectCommand("owner-1", "Alpha", null));
        var existing2 = await mediator.Send(new CreateProjectCommand("owner-1", "Beta", null));

        // Act
        var updateResult = await mediator.Send(new UpdateProjectCommand(existing2.Value!.Id, "Alpha", null));

        // Assert
        existing1.IsSuccess.Should().BeTrue();
        existing2.IsSuccess.Should().BeTrue();
        updateResult.IsSuccess.Should().BeFalse();
        updateResult.Error!.Code.Should().Be("PROJECT_DUPLICATE");
    }

    [Fact]
    public async Task Delete_Removes_Project_And_GetById_Fails()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var createResult = await mediator.Send(new CreateProjectCommand("owner-1", "Gamma", null));

        // Act
        var deleteResult = await mediator.Send(new DeleteProjectCommand(createResult.Value!.Id));
        var getResult = await mediator.Send(new GetProjectByIdQuery(createResult.Value!.Id));

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        deleteResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeFalse();
        getResult.Error!.Code.Should().Be("PROJECT_NOT_FOUND");
    }

    [Fact]
    public async Task GetProjects_Paginates_And_Orders_By_CreatedAt()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new CreateProjectCommand("owner-2", "P1", null));
        await mediator.Send(new CreateProjectCommand("owner-2", "P2", null));
        await mediator.Send(new CreateProjectCommand("owner-2", "P3", null));

        // Act
        var page1 = await mediator.Send(new GetProjectsQuery("owner-2", PageNumber: 1, PageSize: 2));
        var page2 = await mediator.Send(new GetProjectsQuery("owner-2", PageNumber: 2, PageSize: 2));

        // Assert
        page1.IsSuccess.Should().BeTrue();
        page2.IsSuccess.Should().BeTrue();
        page1.Value!.Items.Should().HaveCount(2);
        page2.Value!.Items.Should().HaveCount(1);
        page1.Value!.TotalCount.Should().Be(3);
        page1.Value!.TotalPages.Should().Be(2);
    }
}
