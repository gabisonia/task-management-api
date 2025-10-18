using TaskService.Application.Tasks.Commands.CreateTask;
using TaskService.Application.Tasks.Commands.DeleteTask;
using TaskService.Application.Tasks.Commands.UpdateTask;
using TaskService.Application.Tasks.Queries;
using TaskService.Domain.TaskItemManagement;
using TaskService.IntegrationTests.Fixtures;
using TaskService.Application.Projects.Commands;

namespace TaskService.IntegrationTests.Tasks;

public sealed class TaskHandlersTests : IClassFixture<TestcontainersFixture>
{
    private readonly TestcontainersFixture _fx;

    public TaskHandlersTests(TestcontainersFixture fx) => _fx = fx;

    [Fact]
    public async Task Create_And_GetById_Works()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var projectResult = await mediator.Send(new CreateProjectCommand("owner-10", "Work", null));
        var projectId = projectResult.Value!.Id;

        // Act
        var createResult = await mediator.Send(new CreateTaskCommand(
            ProjectId: projectId,
            Title: "T1",
            Description: "Desc",
            Status: TaskItemStatus.New,
            Priority: Priority.Medium,
            AssigneeUserId: null,
            DueDate: null,
            Tags: []));
        var getResult = await mediator.Send(new GetTaskByIdQuery(createResult.Value!.Id));

        // Assert
        projectResult.IsSuccess.Should().BeTrue();
        createResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value!.Title.Should().Be("T1");
        getResult.Value!.Status.Should().Be(TaskItemStatus.New.ToString());
        getResult.Value!.Priority.Should().Be(Priority.Medium.ToString());
    }

    [Fact]
    public async Task Update_Then_Delete_Task_Behaves_As_Expected()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var projectResult = await mediator.Send(new CreateProjectCommand("owner-11", "Home", null));
        var createResult = await mediator.Send(new CreateTaskCommand(
            ProjectId: projectResult.Value!.Id,
            Title: "Task A",
            Description: null,
            Status: TaskItemStatus.New,
            Priority: Priority.Low,
            AssigneeUserId: null,
            DueDate: null,
            Tags: []));

        // Act
        var updateResult = await mediator.Send(new UpdateTaskCommand(
            Id: createResult.Value!.Id,
            Title: "Task A+",
            Description: "Updated",
            Status: TaskItemStatus.InProgress,
            Priority: Priority.High,
            DueDate: DateTime.UtcNow.Date.AddDays(7),
            Tags: ["u"]));
        var deleteResult = await mediator.Send(new DeleteTaskCommand(createResult.Value!.Id));
        var getResult = await mediator.Send(new GetTaskByIdQuery(createResult.Value!.Id));

        // Assert
        projectResult.IsSuccess.Should().BeTrue();
        createResult.IsSuccess.Should().BeTrue();
        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value!.Title.Should().Be("Task A+");
        updateResult.Value!.Status.Should().Be(TaskItemStatus.InProgress.ToString());
        updateResult.Value!.Priority.Should().Be(Priority.High.ToString());
        deleteResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeFalse();
        getResult.Error!.Code.Should().Be("TASK_NOT_FOUND");
    }

    [Fact]
    public async Task GetTasks_Filters_By_Status_And_Paginates()
    {
        // Arrange
        await using var provider = _fx.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var projectResult = await mediator.Send(new CreateProjectCommand("owner-12", "Ops", null));
        var pid = projectResult.Value!.Id;
        await mediator.Send(new CreateTaskCommand(pid, "A", null, TaskItemStatus.New, Priority.Low, null, null,
            []));
        await mediator.Send(new CreateTaskCommand(pid, "B", null, TaskItemStatus.InProgress, Priority.Medium, null,
            null, []));
        await mediator.Send(new CreateTaskCommand(pid, "C", null, TaskItemStatus.InProgress, Priority.Medium, null,
            null, []));

        // Act
        var listResult = await mediator.Send(new GetTasksQuery(pid, Status: nameof(TaskItemStatus.InProgress),
            PageNumber: 1, PageSize: 10));

        // Assert
        projectResult.IsSuccess.Should().BeTrue();
        listResult.IsSuccess.Should().BeTrue();
        listResult.Value!.Items.Should().OnlyContain(t => t.Status == TaskItemStatus.InProgress.ToString());
        listResult.Value!.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }
}
