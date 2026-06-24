using System.Text.Json;

using Flowist.ActivityService.Data;
using Flowist.ActivityService.Entities;
using Flowist.ActivityService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.ActivityService.Consumers;

public sealed class UserRegisteredEventConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly ActivityDbContext _dbContext;
    private readonly ILogger<UserRegisteredEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public UserRegisteredEventConsumer(
        ActivityDbContext dbContext,
        ILogger<UserRegisteredEventConsumer> logger,
        IProcessedEventService processedEventService)
    {
        _dbContext = dbContext;
        _processedEventService = processedEventService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        try
        {
            UserRegisteredEvent message = context.Message;

            if (await _processedEventService.IsProcessedAsync(message.EventId, context.CancellationToken))
            {
                _logger.LogInformation(
                    "Skipping duplicate event {EventType} with event id {EventId}.",
                    nameof(UserRegisteredEvent),
                    message.EventId);

                return;
            }

            ActivityLog activityLog = new()
            {
                Id = Guid.NewGuid(),
                WorkspaceId = null,
                UserId = message.UserId,
                ActionType = ActivityType.UserRegistered,
                EntityType = "User",
                EntityId = message.UserId,
                Description = $"User '{message.Email}' registered.",
                Metadata = JsonSerializer.Serialize(new
                {
                    message.UserId,
                    message.Email,
                    message.FullName,
                    message.createdAt
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ActivityLogs.Add(activityLog);
            _processedEventService.MarkAsProcessed(message.EventId, nameof(UserRegisteredEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Created activity log {ActivityLogId} for user registered event {EventId}.",
                activityLog.Id,
                message.EventId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to consume {EventType} with message id {MessageId}.",
                nameof(UserRegisteredEvent),
                context.MessageId);

            throw;
        }
    }
}