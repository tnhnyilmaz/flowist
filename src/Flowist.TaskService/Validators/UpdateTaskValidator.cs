using Flowist.Shared.Constants;
using Flowist.TaskService.DTOs;
using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class UpdateTaskValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .WithMessage("Task title is required.")
            .MaximumLength(ValidationConstants.TaskTitleMaxLength)
            .WithMessage($"Task title must not exceed {ValidationConstants.TaskTitleMaxLength} characters.");

        RuleFor(request => request.Description)
            .MaximumLength(ValidationConstants.TaskDescriptionMaxLength)
            .WithMessage($"Task description must not exceed {ValidationConstants.TaskDescriptionMaxLength} characters.");

        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage("Task status is invalid.");

        RuleFor(request => request.Priority)
            .IsInEnum()
            .WithMessage("Task priority is invalid.");

        RuleFor(request => request.AssigneeId)
            .NotEqual(Guid.Empty)
            .When(request => request.AssigneeId.HasValue)
            .WithMessage("Assignee id is invalid.");
    }
}