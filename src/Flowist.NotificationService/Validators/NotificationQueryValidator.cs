using Flowist.NotificationService.DTOs;
using FluentValidation;

namespace Flowist.NotificationService.Validators;

public sealed class NotificationQueryValidator : AbstractValidator<NotificationQueryRequest>
{
    public NotificationQueryValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}