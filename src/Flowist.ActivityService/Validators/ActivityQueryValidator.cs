using Flowist.ActivityService.DTOs;

using FluentValidation;

namespace Flowist.ActivityService.Validators;

public sealed class ActivityQueryValidator : AbstractValidator<ActivityQueryRequest>
{
    public ActivityQueryValidator()
    {
        RuleFor(request => request.ActionType)
            .IsInEnum()
            .When(request => request.ActionType.HasValue)
            .WithMessage("Activity action type is invalid.");

        RuleFor(request => request.UserId)
            .NotEqual(Guid.Empty)
            .When(request => request.UserId.HasValue)
            .WithMessage("User id is invalid.");

        RuleFor(request => request.From)
            .LessThanOrEqualTo(request => request.To)
            .When(request => request.From.HasValue && request.To.HasValue)
            .WithMessage("From date must be less than or equal to To date.");

        RuleFor(request => request.EntityType)
            .MaximumLength(100)
            .WithMessage("Entity type must not exceed 100 characters.");

        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}