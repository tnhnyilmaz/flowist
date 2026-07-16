using Flowist.NotificationService.Data;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace Flowist.NotificationService.Tests.TestSupport;

internal static class NotificationTestFactory
{
    internal static NotificationDbContext CreateDbContext()
    {
        DbContextOptions<NotificationDbContext> options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }

    internal static ConsumeContext<TMessage> CreateConsumeContext<TMessage>(TMessage message)
        where TMessage : class
    {
        Mock<ConsumeContext<TMessage>> context = new();
        context.SetupGet(item => item.Message).Returns(message);
        context.SetupGet(item => item.CancellationToken).Returns(CancellationToken.None);
        context.SetupGet(item => item.MessageId).Returns(Guid.NewGuid());

        return context.Object;
    }
}