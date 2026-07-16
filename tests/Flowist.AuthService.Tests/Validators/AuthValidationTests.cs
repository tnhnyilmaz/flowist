using Flowist.AuthService.DTOs;
using Flowist.AuthService.Validators;
using Flowist.Shared.Constants;

using FluentAssertions;

using FluentValidation.Results;

namespace Flowist.AuthService.Tests.Validators;

public sealed class AuthValidationTests
{
    [Fact]
    public void RegisterRequestValidator_ShouldPass_WhenRequestIsValid()
    {
        RegisterRequestValidator validator = new();

        RegisterRequest request = new(
            "valid-user@flowist.local",
            "Test123!",
            "Valid User");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void RegisterRequestValidator_ShouldFail_WhenEmailIsInvalid(string email)
    {
        RegisterRequestValidator validator = new();

        RegisterRequest request = new(
            email,
            "Test123!",
            "Valid User");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void RegisterRequestValidator_ShouldFail_WhenEmailExceedsMaxLength()
    {
        RegisterRequestValidator validator = new();

        string email = $"{new string('a', ValidationConstants.EmailMaxLength)}@flowist.local";

        RegisterRequest request = new(
            email,
            "Test123!",
            "Valid User");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("lowercase1!")]
    [InlineData("UPPERCASE1!")]
    [InlineData("NoDigits!")]
    [InlineData("NoSpecial1")]
    public void RegisterRequestValidator_ShouldFail_WhenPasswordIsInvalid(string password)
    {
        RegisterRequestValidator validator = new();

        RegisterRequest request = new(
            "valid-user@flowist.local",
            password,
            "Valid User");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void RegisterRequestValidator_ShouldFail_WhenFullNameIsEmpty()
    {
        RegisterRequestValidator validator = new();

        RegisterRequest request = new(
            "valid-user@flowist.local",
            "Test123!",
            "");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.FullName));
    }

    [Fact]
    public void RegisterRequestValidator_ShouldFail_WhenFullNameExceedsMaxLength()
    {
        RegisterRequestValidator validator = new();

        RegisterRequest request = new(
            "valid-user@flowist.local",
            "Test123!",
            new string('a', ValidationConstants.FullNameMaxLength + 1));

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterRequest.FullName));
    }

    [Fact]
    public void LoginRequestValidator_ShouldPass_WhenRequestIsValid()
    {
        LoginRequestValidator validator = new();

        LoginRequest request = new(
            "valid-user@flowist.local",
            "Test123!");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void LoginRequestValidator_ShouldFail_WhenEmailIsInvalid(string email)
    {
        LoginRequestValidator validator = new();

        LoginRequest request = new(email, "Test123!");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginRequest.Email));
    }

    [Fact]
    public void LoginRequestValidator_ShouldFail_WhenPasswordIsEmpty()
    {
        LoginRequestValidator validator = new();

        LoginRequest request = new("valid-user@flowist.local", "");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void RefreshTokenRequestValidator_ShouldPass_WhenRefreshTokenIsProvided()
    {
        RefreshTokenRequestValidator validator = new();

        RefreshTokenRequest request = new("refresh-token");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RefreshTokenRequestValidator_ShouldFail_WhenRefreshTokenIsEmpty()
    {
        RefreshTokenRequestValidator validator = new();

        RefreshTokenRequest request = new("");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RefreshTokenRequest.RefreshToken));
    }

    [Fact]
    public void RevokeTokenRequestValidator_ShouldPass_WhenRefreshTokenIsProvided()
    {
        RevokeTokenRequestValidator validator = new();

        RevokeTokenRequest request = new("refresh-token");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RevokeTokenRequestValidator_ShouldFail_WhenRefreshTokenIsEmpty()
    {
        RevokeTokenRequestValidator validator = new();

        RevokeTokenRequest request = new("");

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RevokeTokenRequest.RefreshToken));
    }
}