using Flowist.TaskService.DTOs;

using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class AddMemberValidator : AbstractValidator<AddWorkspaceMemberRequest>
{
    public AddMemberValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(request => request.Role)
            .IsInEnum()
            .WithMessage("Workspace role is invalid.");
    }
}