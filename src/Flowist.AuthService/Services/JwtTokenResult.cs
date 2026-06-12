namespace Flowist.AuthService.Services;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAt);