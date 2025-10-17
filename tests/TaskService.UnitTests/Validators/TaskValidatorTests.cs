using FluentAssertions;
using FluentValidation.Results;
using TaskService.Application.Dtos.Tasks;
using Xunit;

namespace TaskService.UnitTests.Validators;

public sealed class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task Title",
            Description = "This is a valid description.",
            Priority = "Medium",
            AssigneeUserId = "user123",
            DueDate = DateTime.UtcNow.AddDays(7),
            Tags = ["tag1", "tag2"]
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyTitle_ReturnsError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = string.Empty,
            Priority = "Medium"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("title is required"));
    }

    [Fact]
    public void Validate_TitleTooShort_ReturnsError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "AB", // Only 2 characters
            Priority = "Medium"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 3 and 160 characters"));
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Urgent")]
    public void Validate_ValidPriority_ReturnsSuccess(string priority)
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task",
            Priority = priority
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidPriority_ReturnsError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task",
            Priority = "InvalidPriority"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Low, Medium, High, Urgent"));
    }

    [Fact]
    public void Validate_DueDateInPast_ReturnsError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task",
            Priority = "Medium",
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot be in the past"));
    }

    [Fact]
    public void Validate_TooManyTags_ReturnsError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task",
            Priority = "Medium",
            Tags = Enumerable.Range(1, 11).Select(i => $"tag{i}").ToArray() // 11 tags
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("cannot have more than 10 tags"));
    }

    [Fact]
    public void Validate_TagTooLong_ReturnsError()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "Valid Task",
            Priority = "Medium",
            Tags = [new string('A', 51)] // 51 characters
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 1 and 50 characters"));
    }
}

public sealed class UpdateTaskStatusRequestValidatorTests
{
    private readonly UpdateTaskStatusRequestValidator _validator = new();

    [Theory]
    [InlineData("New")]
    [InlineData("InProgress")]
    [InlineData("Blocked")]
    [InlineData("Done")]
    public void Validate_ValidStatus_ReturnsSuccess(string status)
    {
        // Arrange
        var request = new UpdateTaskStatusRequest
        {
            Status = status
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidStatus_ReturnsError()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest
        {
            Status = "InvalidStatus"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("New, InProgress, Blocked, Done"));
    }

    [Fact]
    public void Validate_EmptyStatus_ReturnsError()
    {
        // Arrange
        var request = new UpdateTaskStatusRequest
        {
            Status = string.Empty
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("status is required"));
    }
}
