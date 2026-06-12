using Flowist.AuthService.Entities;
using Flowist.AuthService.Options;

using System.Security.Claims;

using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Flowist.AuthService.Services;

public sealed class JwtTokenService : IJwtTokenService
{

    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public JwtTokenResult GenerateAccessToken(User user)
    {
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
        Claim[] claims =
         [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(Flowist.Shared.Constants.ClaimTypes.UserId, user.Id.ToString()),
            new(Flowist.Shared.Constants.ClaimTypes.Email, user.Email),
            new(Flowist.Shared.Constants.ClaimTypes.FullName, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
         ];

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new JwtTokenResult(accessToken,expiresAt);

    }

    public string GenerateRefreshTokenString()
    {
        throw new NotImplementedException();
    }
}


