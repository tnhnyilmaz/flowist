using Flowist.TaskService.DTOs;

using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class TaskFilterValidator : AbstractValidator<TaskFilterRequest>
{
    public TaskFilterValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .When(request => request.Status.HasValue)
            .WithMessage("Task status is invalid.");

        RuleFor(request => request.Priority)
            .IsInEnum()
            .When(request => request.Priority.HasValue)
            .WithMessage("Task priority is invalid.");

        RuleFor(request => request.AssigneeId)
            .NotEqual(Guid.Empty)
            .When(request => request.AssigneeId.HasValue)
            .WithMessage("Assignee id is invalid.");

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");

        RuleFor(request => request.SortBy)
            .Must(sortBy =>
                string.IsNullOrWhiteSpace(sortBy) ||
                sortBy.Equals("createdAt", StringComparison.OrdinalIgnoreCase) ||
                sortBy.Equals("dueDate", StringComparison.OrdinalIgnoreCase) ||
                sortBy.Equals("priority", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort by must be one of: createdAt, dueDate, priority.");
    }
}