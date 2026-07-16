using Flowist.NotificationService.Data;
using Flowist.NotificationService.DTOs;
using Flowist.NotificationService.Entities;
using Flowist.Shared.Caching;
using Flowist.Shared.DTOs;
using Flowist.Shared.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace Flowist.NotificationService.Services;

public class NotificationService : INotificationService
{
    private const string UnreadCountCacheKeyPrefix = "notification:unread-count:";
    private static readonly TimeSpan UnreadCountCacheExpiration = CacheExpirationDefaults.NotificationUnreadCount;
    private readonly NotificationDbContext _dbContext;
    private readonly INotificationRealtimeService _realtimeService;
    private readonly ICacheService _cacheService;
    public NotificationService(NotificationDbContext dbContext, INotificationRealtimeService notificationRealtimeService, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _realtimeService = notificationRealtimeService;
        _cacheService = cacheService;
    }
    public async Task DeleteAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        Notification notification = await GetUserNotificationAsync(notificationId, userId, cancellationToken);

        _dbContext.Notifications.Remove(notification);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendUnreadCountAsync(userId, cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(Guid userId, NotificationQueryRequest request, CancellationToken cancellationToken = default)
    {
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        IQueryable<Notification> query = _dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Notification> notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(
            notifications.Select(ToNotificationDto).ToArray(),
            totalCount,
            page,
            pageSize
        );
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        string cacheKey = GetUnreadCountCacheKey(userId);

        int? cachedUnreadCount = await _cacheService.GetAsync<int?>(
            cacheKey,
            cancellationToken);

        if (cachedUnreadCount.HasValue)
        {
            return cachedUnreadCount.Value;
        }

        return await RefreshUnreadCountCacheAsync(userId, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications
             .Where(notification => notification.UserId == userId && !notification.IsRead)
             .ExecuteUpdateAsync(setters => setters
             .SetProperty(notification => notification.IsRead, true)
             .SetProperty(notification => notification.ReadAt, DateTimeOffset.UtcNow),
             cancellationToken);
        await SendUnreadCountAsync(userId, cancellationToken);
    }



    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        Notification notification = await GetUserNotificationAsync(notificationId, userId, cancellationToken);

        if (notification.IsRead) return;

        notification.IsRead = true;

        notification.ReadAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendUnreadCountAsync(userId, cancellationToken);
    }

    private async Task SendUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        int unreadCount = await RefreshUnreadCountCacheAsync(userId, cancellationToken);

        await _realtimeService.SendUnreadCountAsync(
            userId,
            unreadCount,
            cancellationToken);
    }
    private async Task<Notification> GetUserNotificationAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(notification => notification.Id == notificationId && notification.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), notificationId);
    }
    private static NotificationDto ToNotificationDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt);
    }
    private async Task<int> RefreshUnreadCountCacheAsync(Guid userId, CancellationToken cancellationToken)
    {
        int unreadCount = await _dbContext.Notifications
            .CountAsync(
                notification => notification.UserId == userId && !notification.IsRead,
                cancellationToken);

        await _cacheService.SetAsync(
            GetUnreadCountCacheKey(userId),
            unreadCount,
            UnreadCountCacheExpiration,
            cancellationToken);

        return unreadCount;
    }

    private static string GetUnreadCountCacheKey(Guid userId)
    {
        return $"{UnreadCountCacheKeyPrefix}{userId}";
    }
}