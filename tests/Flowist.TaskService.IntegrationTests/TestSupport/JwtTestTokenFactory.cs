using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Flowist.TaskService.IntegrationTests.TestSupport;

public static class JwtTestTokenFactory
{
    public const string Issuer = "Flowist.AuthService.Tests";
    public const string Audience = "Flowist.IntegrationTests";
    public const string SecretKey = "INTEGRATION_TEST_SECRET_KEY_AT_LEAST_32_CHARS";

    public static AuthenticationHeaderValue CreateAuthorizationHeader(Guid userId)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(Flowist.Shared.Constants.ClaimTypes.UserId, userId.ToString()),
            new(Flowist.Shared.Constants.ClaimTypes.Email, $"{userId:N}@flowist.local"),
            new(Flowist.Shared.Constants.ClaimTypes.FullName, "Integration Test User"),
            new(JwtRegisteredClaimNames.Email, $"{userId:N}@flowist.local"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(SecretKey));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthenticationHeaderValue("Bearer", accessToken);
    }
}