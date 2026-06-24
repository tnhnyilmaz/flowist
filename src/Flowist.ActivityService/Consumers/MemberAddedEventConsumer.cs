using System.Text.Json;

using Flowist.ActivityService.Data;
using Flowist.ActivityService.Entities;
using Flowist.ActivityService.Services;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using MassTransit;

namespace Flowist.ActivityService.Consumers;

public sealed class MemberAddedEventConsumer : IConsumer<MemberAddedEvent>
{
    private readonly ActivityDbContext _dbContext;
    private readonly ILogger<MemberAddedEventConsumer> _logger;
    private readonly IProcessedEventService _processedEventService;

    public MemberAddedEventConsumer(
        ActivityDbContext dbContext,
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
                    nameof(TaskCreatedEvent),
                    message.EventId);

                return;
            }

            ActivityLog activityLog = new()
            {
                Id = Guid.NewGuid(),
                WorkspaceId = message.WorkspaceId,
                UserId = message.AddedBy,
                ActionType = ActivityType.MemberAdded,
                EntityType = "WorkspaceMember",
                EntityId = message.UserId,
                Description = $"User {message.UserId} was added to workspace as {message.Role}.",
                Metadata = JsonSerializer.Serialize(new
                {
                    message.WorkspaceId,
                    message.UserId,
                    message.Role,
                    message.AddedBy,
                    message.AddedAt
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.ActivityLogs.Add(activityLog);

            _processedEventService.MarkAsProcessed(message.EventId, nameof(TaskCreatedEvent));

            await _dbContext.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "Created activity log {ActivityLogId} for member added event {EventId}.",
                activityLog.Id,
                message.EventId);
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