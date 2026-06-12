using Flowist.AuthService.DTOs;
using Flowist.Shared.DTOs;

namespace Flowist.AuthService.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? deviceInfo, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, string? deviceInfo, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? deviceInfo, CancellationToken cancellationToken = default);

    Task RevokeTokenAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}