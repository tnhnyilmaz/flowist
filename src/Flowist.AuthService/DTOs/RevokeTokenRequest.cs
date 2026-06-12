namespace Flowist.AuthService.DTOs;

public sealed record RevokeTokenRequest(
    string RefreshToken);