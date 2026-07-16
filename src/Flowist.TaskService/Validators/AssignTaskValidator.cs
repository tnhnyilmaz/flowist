using Flowist.TaskService.DTOs;

using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class AssignTaskValidator : AbstractValidator<AssignTaskRequest>
{
    public AssignTaskValidator()
    {
        RuleFor(request => request.AssigneeId)
            .NotEmpty()
            .WithMessage("Assignee id is required.");
    }
}