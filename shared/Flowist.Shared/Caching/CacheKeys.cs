namespace Flowist.Shared.Caching;

public static class CacheKeys
{
    public static string JwtBlacklist(string tokenId)
    {
        return $"auth:blacklist:jwt:{tokenId}";
    }

    public static string RefreshToken(string tokenHash)
    {
        return $"auth:refresh-token:{tokenHash}";
    }

    public static string NotificationUnreadCount(Guid userId)
    {
        return $"notification:unread-count:{userId}";
    }

    public static string SignalRUserConnections(Guid userId)
    {
        return $"signalr:user-connections:{userId}";
    }
}