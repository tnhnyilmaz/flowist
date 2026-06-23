using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

using Microsoft.EntityFrameworkCore;

namespace Flowist.NotificationService.Consumers;

public sealed class TaskCreatedEventConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<TaskCreatedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;
    private readonly INotificationRealtimeService _realtimeService;


    public TaskCreatedEventConsumer(
        NotificationDbContext dbContext,
        ILogger<TaskCreatedEventConsumer> logger,
        IProcessedEventService processedEventService, INotificationRealtimeService realtimeService
        )
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
        _realtimeService = realtimeService;

    }
    public async Task Consume(ConsumeContext<TaskCreatedEvent> context)
    {
        try
        {
            TaskCreatedEvent message = context.Message;

            if (await _processedEventService.IsProcessedAsync(message.EventId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Skipping duplicate event {EventType} with event id {EventId}.",
                    nameof(TaskCreatedEvent),
                    message.EventId);

                return;
            }

            Notification notification = new()
            {
                Id = Guid.NewGuid(),
                UserId = message.CreatedBy,
                Type = NotificationType.TaskUpdated,
                Title = "Task created",
                Message = $"Task '{message.Title}' was created.",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            _processedEventService.MarkAsProcessed(message.EventId, nameof(TaskCreatedEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            int unreadCount = await _dbContext.Notifications
                .CountAsync
                        (existingNotification =>
                                        existingNotification.UserId == notification.UserId && !existingNotification.IsRead,
                                        context.CancellationToken);

            await _realtimeService.SendUnreadCountAsync(
                notification.UserId,
                unreadCount,
                context.CancellationToken);

            NotificationDto notificationDto = new(
                notification.Id,
                notification.UserId,
                notification.Type,
                notification.Message,
                notification.IsRead,
                notification.CreatedAt);

            await _realtimeService.SendNotificationAsync(notificationDto, context.CancellationToken);
            _logger.LogInformation(
                "Created notification {NotificationId} for created task {TaskId}.",
                notification.Id,
                message.TaskId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume {EventType} with message id {MessageId}.",
                nameof(TaskCreatedEvent),
                context.MessageId);

            throw;
        }
    }
}