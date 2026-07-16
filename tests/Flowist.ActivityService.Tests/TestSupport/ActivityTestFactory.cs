using Flowist.ActivityService.Data;

using MassTransit;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace Flowist.ActivityService.Tests.TestSupport;

internal static class ActivityTestFactory
{
    internal static ActivityDbContext CreateDbContext()
    {
        DbContextOptions<ActivityDbContext> options = new DbContextOptionsBuilder<ActivityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ActivityDbContext(options);
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