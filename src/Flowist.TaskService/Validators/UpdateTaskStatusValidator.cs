using Flowist.TaskService.DTOs;
using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class UpdateTaskStatusValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .WithMessage("Task status is invalid.");
    }
}