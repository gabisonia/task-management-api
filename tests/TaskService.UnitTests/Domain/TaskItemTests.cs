using FluentAssertions;
using MongoDB.Bson;
using TaskService.Domain.TaskItemManagement;

namespace TaskService.UnitTests.Domain;

public class TaskItemTests
{
    [Fact]
    public void TaskItem_CanBeCreated_WithValidData()
    {
        // Arrange
        var taskId = ObjectId.GenerateNewId();
        var projectId = ObjectId.GenerateNewId();
        string userId = "user-456";
        DateTime now = DateTime.UtcNow;

        // Act
        var task = new TaskItem
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "A test task",
            Status = TaskItemStatus.New,
            Priority = Priority.Medium,
            AssigneeUserId = userId,
            DueDate = now.AddDays(7),
            Tags = ["test", "unit"],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId,
            IsDeleted = false
        };

        // Assert
        task.Should().NotBeNull();
        task.Id.Should().Be(taskId);
        task.ProjectId.Should().Be(projectId);
        task.Title.Should().Be("Test Task");
        task.Status.Should().Be(TaskItemStatus.New);
        task.Priority.Should().Be(Priority.Medium);
        task.AssigneeUserId.Should().Be(userId);
        task.Tags.Should().HaveCount(2);
        task.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void TaskItem_ImplementsIHasAudit()
    {
        // Arrange & Act
        var task = new TaskItem();

        // Assert
        task.Should().BeAssignableTo<TaskService.Domain.Common.IHasAudit>();
    }

    [Fact]
    public void TaskItem_Status_CanTransition_FromNewToInProgress()
    {
        // Arrange
        var task = new TaskItem { Status = TaskItemStatus.New };

        // Act
        task.Status = TaskItemStatus.InProgress;

        // Assert
        task.Status.Should().Be(TaskItemStatus.InProgress);
    }

    [Fact]
    public void TaskItem_CanBeUnassigned()
    {
        // Arrange
        var task = new TaskItem { AssigneeUserId = "user-123" };

        // Act
        task.AssigneeUserId = null;

        // Assert
        task.AssigneeUserId.Should().BeNull();
    }

    [Fact]
    public void TaskItem_Tags_CanBeEmpty()
    {
        // Arrange & Act
        var task = new TaskItem { Tags = [] };

        // Assert
        task.Tags.Should().BeEmpty();
    }

    [Fact]
    public void TaskItem_Priority_CanBeUrgent()
    {
        // Arrange
        var task = new TaskItem { Priority = Priority.Low };

        // Act
        task.Priority = Priority.Urgent;

        // Assert
        task.Priority.Should().Be(Priority.Urgent);
    }
}
