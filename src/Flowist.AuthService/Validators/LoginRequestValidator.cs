using Flowist.AuthService.DTOs;
using Flowist.Shared.Constants;
using FluentValidation;

namespace Flowist.AuthService.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(ValidationConstants.EmailMaxLength)
            .WithMessage($"Email must not exceed {ValidationConstants.EmailMaxLength} characters.")
            .EmailAddress()
            .WithMessage("Email format is invalid.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}