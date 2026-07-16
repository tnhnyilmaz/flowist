using Flowist.NotificationService.Consumers;
using Flowist.NotificationService.Services;
using Flowist.NotificationService.Tests.TestSupport;
using Flowist.Shared.DTOs;
using Flowist.Shared.Enums;
using Flowist.Shared.Events;

using FluentAssertions;

using MassTransit;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace Flowist.NotificationService.Tests.Consumers;

public sealed class NotificationConsumerTests
{
    [Fact]
    public async Task TaskCreatedEventConsumer_ShouldCreateNotificationAndSendRealtimeMessages()
    {
        await using var dbContext = NotificationTestFactory.CreateDbContext();
        ProcessedEventService processedEventService = new(dbContext);
        Mock<INotificationRealtimeService> realtimeService = new();
        TaskCreatedEvent message = new(
            Guid.NewGuid(),
            "Important task",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        TaskCreatedEventConsumer consumer = new(
            dbContext,
            NullLogger<TaskCreatedEventConsumer>.Instance,
            processedEventService,
            realtimeService.Object);

        ConsumeContext<TaskCreatedEvent> context = NotificationTestFactory.CreateConsumeContext(message);

        await consumer.Consume(context);

        dbContext.Notifications.Should().ContainSingle(notification =>
            notification.UserId == message.CreatedBy &&
            notification.Type == NotificationType.TaskUpdated &&
            notification.Message.Contains(message.Title));

        dbContext.ProcessedEvents.Should().ContainSingle(processedEvent => processedEvent.EventId == message.EventId);

        realtimeService.Verify(service => service.SendUnreadCountAsync(
            message.CreatedBy,
            1,
            It.IsAny<CancellationToken>()), Times.Once);

        realtimeService.Verify(service => service.SendNotificationAsync(
            It.Is<NotificationDto>(notification => notification.UserId == message.CreatedBy),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TaskCreatedEventConsumer_ShouldSkipDuplicateEvent()
    {
        await using var dbContext = NotificationTestFactory.CreateDbContext();
        ProcessedEventService processedEventService = new(dbContext);
        Mock<INotificationRealtimeService> realtimeService = new();
        TaskCreatedEvent message = new(
            Guid.NewGuid(),
            "Important task",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        processedEventService.MarkAsProcessed(message.EventId, nameof(TaskCreatedEvent));
        await dbContext.SaveChangesAsync();

        TaskCreatedEventConsumer consumer = new(
            dbContext,
            NullLogger<TaskCreatedEventConsumer>.Instance,
            processedEventService,
            realtimeService.Object);

        await consumer.Consume(NotificationTestFactory.CreateConsumeContext(message));

        dbContext.Notifications.Should().BeEmpty();
        realtimeService.Verify(service => service.SendNotificationAsync(
            It.IsAny<NotificationDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TaskAssignedEventConsumer_ShouldCreateNotificationForAssignee()
    {
        await using var dbContext = NotificationTestFactory.CreateDbContext();
        ProcessedEventService processedEventService = new(dbContext);
        Mock<INotificationRealtimeService> realtimeService = new();
        Guid assignedTo = Guid.Parse("22222222-2222-2222-2222-222222222222");
        TaskAssignedEvent message = new(
            Guid.NewGuid(),
            assignedTo,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        TaskAssignedEventConsumer consumer = new(
            dbContext,
            NullLogger<TaskAssignedEventConsumer>.Instance,
            processedEventService,
            realtimeService.Object);

        await consumer.Consume(NotificationTestFactory.CreateConsumeContext(message));

        dbContext.Notifications.Should().ContainSingle(notification =>
            notification.UserId == assignedTo &&
            notification.Type == NotificationType.TaskAssigned &&
            notification.Message == "A task was assigned to you.");

        dbContext.ProcessedEvents.Should().ContainSingle(processedEvent => processedEvent.EventId == message.EventId);

        realtimeService.Verify(service => service.SendUnreadCountAsync(
            assignedTo,
            1,
            It.IsAny<CancellationToken>()), Times.Once);

        realtimeService.Verify(service => service.SendNotificationAsync(
            It.Is<NotificationDto>(notification => notification.UserId == assignedTo),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}