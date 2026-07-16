using Flowist.TaskService.DTOs;

using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class UpdateWorkspaceMemberRoleValidator : AbstractValidator<UpdateWorkspaceMemberRoleRequest>
{
    public UpdateWorkspaceMemberRoleValidator()
    {
        RuleFor(request => request.Role)
            .IsInEnum()
            .WithMessage("Workspace role is invalid.");
    }
}