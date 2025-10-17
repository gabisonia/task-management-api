using FluentValidation;

namespace TaskService.Application.Dtos.Projects;

public sealed class UpdateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .Length(3, 120).WithMessage("Project name must be between 3 and 120 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Project description cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Project status is required.")
            .Must(status => status is "Active" or "Archived")
            .WithMessage("Project status must be either 'Active' or 'Archived'.");
    }
}

