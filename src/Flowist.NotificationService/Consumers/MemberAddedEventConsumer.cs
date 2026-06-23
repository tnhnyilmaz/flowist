using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace Flowist.NotificationService.Consumers;

public sealed class MemberAddedEventConsumer : IConsumer<MemberAddedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<MemberAddedEventConsumer> _logger;

    private readonly IProcessedEventService _processedEventService;
    private readonly INotificationRealtimeService _realtimeService;


    public MemberAddedEventConsumer(
        NotificationDbContext dbContext,
        ILogger<MemberAddedEventConsumer> logger,
         IProcessedEventService processedEventService, INotificationRealtimeService realtimeService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
        _realtimeService = realtimeService;

    }

    public async Task Consume(ConsumeContext<MemberAddedEvent> context)
    {
        try
        {
            MemberAddedEvent message = context.Message;

            if (await _processedEventService.IsProcessedAsync(message.EventId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Skipping duplicate event {EventType} with event id {EventId}.",
                    nameof(MemberAddedEvent),
                    message.EventId);

                return;
            }

            Notification notification = new()
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                Type = NotificationType.MemberAdded,
                Title = "Workspace member added",
                Message = $"You were added to a workspace as {message.Role}.",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            _processedEventService.MarkAsProcessed(message.EventId, nameof(MemberAddedEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);
            NotificationDto notificationDto = new(
                           notification.Id,
                           notification.UserId,
                           notification.Type,
                           notification.Message,
                           notification.IsRead,
                           notification.CreatedAt);


            await _realtimeService.SendNotificationAsync(notificationDto, context.CancellationToken);
            int unreadCount = await _dbContext.Notifications
                .CountAsync(existingNotification =>
                existingNotification.UserId == notification.UserId && !existingNotification.IsRead,
                context.CancellationToken);

            await _realtimeService.SendUnreadCountAsync(
                notification.UserId,
                unreadCount,
                context.CancellationToken);


            _logger.LogInformation(
                "Created notification {NotificationId} for workspace member {UserId}.",
                notification.Id,
                message.UserId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume {EventType} with message id {MessageId}.",
                nameof(MemberAddedEvent),
                context.MessageId);

            throw;
        }
    }
}