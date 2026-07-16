namespace Flowist.Shared.Caching;

public static class CacheExpirationDefaults
{
    public static readonly TimeSpan NotificationUnreadCount = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan SignalRConnectionState = TimeSpan.FromHours(12);

    public static readonly TimeSpan ShortLived = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan MediumLived = TimeSpan.FromMinutes(30);

    public static readonly TimeSpan LongLived = TimeSpan.FromHours(12);
}