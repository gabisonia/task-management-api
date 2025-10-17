using FluentAssertions;
using MongoDB.Bson;
using TaskService.Domain.ProjectManagement;

namespace TaskService.UnitTests.Domain;

public class ProjectTests
{
    [Fact]
    public void Project_CanBeCreated_WithValidData()
    {
        // Arrange
        var projectId = ObjectId.GenerateNewId();
        string ownerId = "user-123";
        DateTime now = DateTime.UtcNow;

        // Act
        var project = new Project
        {
            Id = projectId,
            OwnerId = ownerId,
            Name = "Test Project",
            Description = "A test project",
            Status = ProjectStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = ownerId,
            UpdatedBy = ownerId,
            IsDeleted = false
        };

        // Assert
        project.Should().NotBeNull();
        project.Id.Should().Be(projectId);
        project.OwnerId.Should().Be(ownerId);
        project.Name.Should().Be("Test Project");
        project.Description.Should().Be("A test project");
        project.Status.Should().Be(ProjectStatus.Active);
        project.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Project_ImplementsIHasAudit()
    {
        // Arrange & Act
        var project = new Project();

        // Assert
        project.Should().BeAssignableTo<TaskService.Domain.Common.IHasAudit>();
    }

    [Fact]
    public void Project_SoftDelete_SetsIsDeletedFlag()
    {
        // Arrange
        var project = new Project
        {
            Id = ObjectId.GenerateNewId(),
            Name = "Test",
            IsDeleted = false
        };

        // Act
        project.IsDeleted = true;

        // Assert
        project.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Project_Status_CanBeArchived()
    {
        // Arrange
        var project = new Project
        {
            Status = ProjectStatus.Active
        };

        // Act
        project.Status = ProjectStatus.Archived;

        // Assert
        project.Status.Should().Be(ProjectStatus.Archived);
    }
}
