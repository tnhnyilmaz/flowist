using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.NotificationService.Consumers;

public sealed class TaskCreatedEventConsumer : IConsumer<TaskCreatedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<TaskCreatedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public TaskCreatedEventConsumer(
        NotificationDbContext dbContext,
        ILogger<TaskCreatedEventConsumer> logger,
        IProcessedEventService processedEventService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;
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