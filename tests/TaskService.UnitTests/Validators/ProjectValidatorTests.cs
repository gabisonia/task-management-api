using FluentAssertions;
using FluentValidation.Results;
using TaskService.Application.Dtos.Projects;
using Xunit;

namespace TaskService.UnitTests.Validators;

public sealed class CreateProjectRequestValidatorTests
{
    private readonly CreateProjectRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "Valid Project Name",
            Description = "This is a valid description."
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyName_ReturnsError()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = string.Empty,
            Description = "Description"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("name is required"));
    }

    [Fact]
    public void Validate_NameTooShort_ReturnsError()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "AB", // Only 2 characters
            Description = "Description"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("between 3 and 120 characters");
    }

    [Fact]
    public void Validate_NameTooLong_ReturnsError()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = new string('A', 121), // 121 characters
            Description = "Description"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("between 3 and 120 characters");
    }

    [Fact]
    public void Validate_DescriptionTooLong_ReturnsError()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "Valid Name",
            Description = new string('A', 2001) // 2001 characters
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("cannot exceed 2000 characters");
    }

    [Fact]
    public void Validate_NullDescription_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateProjectRequest
        {
            Name = "Valid Name",
            Description = null
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

public sealed class UpdateProjectRequestValidatorTests
{
    private readonly UpdateProjectRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new UpdateProjectRequest
        {
            Name = "Updated Project Name",
            Description = "Updated description",
            Status = "Active"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Archived")]
    public void Validate_ValidStatus_ReturnsSuccess(string status)
    {
        // Arrange
        var request = new UpdateProjectRequest
        {
            Name = "Valid Name",
            Description = "Description",
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
        var request = new UpdateProjectRequest
        {
            Name = "Valid Name",
            Description = "Description",
            Status = "InvalidStatus"
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("Active' or 'Archived");
    }

    [Fact]
    public void Validate_EmptyStatus_ReturnsError()
    {
        // Arrange
        var request = new UpdateProjectRequest
        {
            Name = "Valid Name",
            Description = "Description",
            Status = string.Empty
        };

        // Act
        ValidationResult? result = _validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("status is required"));
    }
}
