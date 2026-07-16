using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Flowist.AuthService.Entities;
using Flowist.AuthService.Options;
using Flowist.AuthService.Services;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace Flowist.AuthService.Tests.Services;

public sealed class JwtTokenServiceTests
{
    private const string Issuer = "Flowist.AuthService";
    private const string Audience = "Flowist.Client";
    private const string SecretKey = "TEST_SECRET_KEY_FOR_JWT_TOKEN_TESTS_32_CHARS_MINIMUM";

    [Fact]
    public void GenerateAccessToken_ShouldReturnTokenAndExpiration()
    {
        JwtTokenService service = CreateService();

        User user = CreateUser();

        DateTimeOffset beforeGenerate = DateTimeOffset.UtcNow;

        JwtTokenResult result = service.GenerateAccessToken(user);

        DateTimeOffset afterGenerate = DateTimeOffset.UtcNow;

        result.AccessToken.Should().NotBeNullOrWhiteSpace();

        result.ExpiresAt.Should().BeAfter(beforeGenerate.AddMinutes(59));
        result.ExpiresAt.Should().BeBefore(afterGenerate.AddMinutes(61));
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeExpectedClaims()
    {
        JwtTokenService service = CreateService();

        User user = CreateUser();

        JwtTokenResult result = service.GenerateAccessToken(user);

        JwtSecurityToken token = ReadToken(result.AccessToken);

        token.Claims.Should().Contain(claim =>
            claim.Type == JwtRegisteredClaimNames.Sub &&
            claim.Value == user.Id.ToString());

        token.Claims.Should().Contain(claim =>
            claim.Type == Flowist.Shared.Constants.ClaimTypes.UserId &&
            claim.Value == user.Id.ToString());

        token.Claims.Should().Contain(claim =>
            claim.Type == Flowist.Shared.Constants.ClaimTypes.Email &&
            claim.Value == user.Email);

        token.Claims.Should().Contain(claim =>
            claim.Type == Flowist.Shared.Constants.ClaimTypes.FullName &&
            claim.Value == user.FullName);

        token.Claims.Should().Contain(claim =>
            claim.Type == JwtRegisteredClaimNames.Email &&
            claim.Value == user.Email);

        Claim? jtiClaim = token.Claims.FirstOrDefault(claim =>
            claim.Type == JwtRegisteredClaimNames.Jti);

        jtiClaim.Should().NotBeNull();
        Guid.TryParse(jtiClaim!.Value, out Guid parsedJti).Should().BeTrue();
        parsedJti.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateAccessToken_ShouldUseConfiguredIssuerAndAudience()
    {
        JwtTokenService service = CreateService();

        User user = CreateUser();

        JwtTokenResult result = service.GenerateAccessToken(user);

        JwtSecurityToken token = ReadToken(result.AccessToken);

        token.Issuer.Should().Be(Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(Audience);
    }

    [Fact]
    public void GenerateAccessToken_ShouldBeSignedWithConfiguredSecretKey()
    {
        JwtTokenService service = CreateService();

        User user = CreateUser();

        JwtTokenResult result = service.GenerateAccessToken(user);

        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        JwtSecurityTokenHandler handler = new();

        ClaimsPrincipal principal = handler.ValidateToken(
            result.AccessToken,
            validationParameters,
            out SecurityToken validatedToken);

        principal.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();

        validatedToken.Should().BeOfType<JwtSecurityToken>();
    }

    private static JwtTokenService CreateService()
    {
        JwtOptions options = new()
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = SecretKey,
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };

        return new JwtTokenService(Microsoft.Extensions.Options.Options.Create(options));
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "jwt-test@flowist.local",
            FullName = "JWT Test User",
            PasswordHash = "hashed-password",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static JwtSecurityToken ReadToken(string accessToken)
    {
        JwtSecurityTokenHandler handler = new();

        return handler.ReadJwtToken(accessToken);
    }
}