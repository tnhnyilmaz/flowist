using Flowist.AuthService.Entities;

namespace Flowist.AuthService.Services;

public interface IJwtTokenService
{
    JwtTokenResult GenerateAccessToken(User user);
    string GenerateRefreshTokenString();
}