using Flowist.Shared.Constants;
using Flowist.TaskService.DTOs;

using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceRequest>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Workspace name is required.")
            .MaximumLength(ValidationConstants.WorkspaceNameMaxLength)
            .WithMessage($"Workspace name must not exceed {ValidationConstants.WorkspaceNameMaxLength} characters.");

        RuleFor(request => request.Description)
            .MaximumLength(ValidationConstants.WorkspaceDescriptionMaxLength)
            .WithMessage($"Workspace description must not exceed {ValidationConstants.WorkspaceDescriptionMaxLength} characters.");
    }
}