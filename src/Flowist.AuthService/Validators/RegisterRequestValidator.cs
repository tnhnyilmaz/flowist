using Flowist.AuthService.DTOs;
using Flowist.Shared.Constants;

using FluentValidation;

namespace Flowist.AuthService.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{


    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MaximumLength(ValidationConstants.EmailMaxLength)
            .WithMessage($"Email must not exceed {ValidationConstants.EmailMaxLength} characters.")
            .EmailAddress()
            .WithMessage("Email format is invalid.");


        RuleFor(request => request.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(ValidationConstants.FullNameMaxLength)
            .WithMessage($"Full name must not exceed {ValidationConstants.FullNameMaxLength} characters.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(ValidationConstants.PasswordMinLength)
            .WithMessage($"Password must be at least {ValidationConstants.PasswordMinLength} characters.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]")
            .WithMessage("Password must contain at least one special character.");
    }




}