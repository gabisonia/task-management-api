using FluentValidation;

namespace TaskService.Application.Dtos.Tasks;

public sealed class UpdateTaskStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Task status is required.")
            .Must(status => status is "New" or "InProgress" or "Blocked" or "Done")
            .WithMessage("Task status must be one of: New, InProgress, Blocked, Done.");
    }
}

