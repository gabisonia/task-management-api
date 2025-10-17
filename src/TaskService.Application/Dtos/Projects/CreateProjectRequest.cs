using FluentValidation;

namespace TaskService.Application.Dtos.Projects;

public sealed class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .Length(3, 120).WithMessage("Project name must be between 3 and 120 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Project description cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}

