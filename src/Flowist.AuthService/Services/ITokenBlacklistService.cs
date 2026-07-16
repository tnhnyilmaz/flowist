namespace Flowist.AuthService.Services;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(
        string tokenId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlacklistedAsync(
        string tokenId,
        CancellationToken cancellationToken = default);
}