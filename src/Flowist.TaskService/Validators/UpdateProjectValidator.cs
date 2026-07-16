using Flowist.Shared.Constants;
using Flowist.TaskService.DTOs;

using FluentValidation;

namespace Flowist.TaskService.Validators;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(ValidationConstants.ProjectNameMaxLength)
            .WithMessage($"Project name must not exceed {ValidationConstants.ProjectNameMaxLength} characters.");

        RuleFor(request => request.Description)
            .MaximumLength(ValidationConstants.ProjectDescriptionMaxLength)
            .WithMessage($"Project description must not exceed {ValidationConstants.ProjectDescriptionMaxLength} characters.");
    }
}