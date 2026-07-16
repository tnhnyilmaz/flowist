using Flowist.ActivityService.Consumers;
using Flowist.ActivityService.Services;
using Flowist.ActivityService.Tests.TestSupport;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace Flowist.ActivityService.Tests.Consumers;

public sealed class ActivityConsumerTests
{
    [Fact]
    public async Task TaskCreatedEventConsumer_ShouldCreateActivityLogAndProcessedEvent()
    {
        await using var dbContext = ActivityTestFactory.CreateDbContext();
        ProcessedEventService processedEventService = new(dbContext);
        TaskCreatedEvent message = new(
            Guid.NewGuid(),
            "Important task",
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        TaskCreatedEventConsumer consumer = new(
            dbContext,
            NullLogger<TaskCreatedEventConsumer>.Instance,
            processedEventService);

        await consumer.Consume(ActivityTestFactory.CreateConsumeContext(message));

        dbContext.ActivityLogs.Should().ContainSingle(activityLog =>
            activityLog.WorkspaceId == message.WorkspaceId &&
            activityLog.UserId == message.CreatedBy &&
            activityLog.ActionType == ActivityType.TaskCreated &&
            activityLog.EntityType == "Task" &&
            activityLog.EntityId == message.TaskId &&
            activityLog.Description.Contains(message.Title));

        dbContext.ProcessedEvents.Should().ContainSingle(processedEvent => processedEvent.EventId == message.EventId);
    }

    [Fact]
    public async Task TaskCreatedEventConsumer_ShouldSkipDuplicateEvent()
    {
        await using var dbContext = ActivityTestFactory.CreateDbContext();
        ProcessedEventService processedEventService = new(dbContext);
        TaskCreatedEvent message = new(
            Guid.NewGuid(),
            "Important task",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        processedEventService.MarkAsProcessed(message.EventId, nameof(TaskCreatedEvent));
        await dbContext.SaveChangesAsync();

        TaskCreatedEventConsumer consumer = new(
            dbContext,
            NullLogger<TaskCreatedEventConsumer>.Instance,
            processedEventService);

        await consumer.Consume(ActivityTestFactory.CreateConsumeContext(message));

        dbContext.ActivityLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task UserRegisteredEventConsumer_ShouldCreateUserRegisteredActivityLog()
    {
        await using var dbContext = ActivityTestFactory.CreateDbContext();
        ProcessedEventService processedEventService = new(dbContext);
        Guid userId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        UserRegisteredEvent message = new(
            userId,
            "activity-user@flowist.local",
            "Activity User",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        UserRegisteredEventConsumer consumer = new(
            dbContext,
            NullLogger<UserRegisteredEventConsumer>.Instance,
            processedEventService);

        await consumer.Consume(ActivityTestFactory.CreateConsumeContext(message));

        dbContext.ActivityLogs.Should().ContainSingle(activityLog =>
            activityLog.WorkspaceId == null &&
            activityLog.UserId == userId &&
            activityLog.ActionType == ActivityType.UserRegistered &&
            activityLog.EntityType == "User" &&
            activityLog.EntityId == userId &&
            activityLog.Description.Contains(message.Email));

        dbContext.ProcessedEvents.Should().ContainSingle(processedEvent => processedEvent.EventId == message.EventId);
    }
}