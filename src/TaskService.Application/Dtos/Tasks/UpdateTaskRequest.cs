using FluentValidation;

namespace TaskService.Application.Dtos.Tasks;

public sealed class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssigneeUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public string[]? Tags { get; set; }
}

public sealed class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .Length(3, 160).WithMessage("Task title must be between 3 and 160 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(4000).WithMessage("Task description cannot exceed 4000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Task status is required.")
            .Must(s => s is "New" or "InProgress" or "Blocked" or "Done")
            .WithMessage("Task status must be one of: New, InProgress, Blocked, Done.");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Task priority is required.")
            .Must(p => p is "Low" or "Medium" or "High" or "Urgent")
            .WithMessage("Task priority must be one of: Low, Medium, High, Urgent.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Due date cannot be in the past.")
            .When(x => x.DueDate.HasValue);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.Length <= 10).WithMessage("A task cannot have more than 10 tags.")
            .When(x => x.Tags != null);

        RuleForEach(x => x.Tags)
            .Length(1, 50).WithMessage("Each tag must be between 1 and 50 characters.")
            .When(x => x.Tags != null && x.Tags.Length > 0);
    }
}

