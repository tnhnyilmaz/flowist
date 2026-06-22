using Flowist.NotificationService.Data;
using Flowist.NotificationService.Entities;
using Flowist.NotificationService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.NotificationService.Consumers;

public sealed class MemberAddedEventConsumer : IConsumer<MemberAddedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<MemberAddedEventConsumer> _logger;

    private readonly IProcessedEventService _processedEventService;

    public MemberAddedEventConsumer(
        NotificationDbContext dbContext,
        ILogger<MemberAddedEventConsumer> logger,
         IProcessedEventService processedEventService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _processedEventService = processedEventService;

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