using AnconaNotificationHub.Contracts;
using Application.Exceptions;
using Application.Features.DispatchEvent;
using Application.Settings;
using Application.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Features;

public class DispatchNotificationEventHandlerTests
{
    private readonly FakeRealtimeNotifier _notifier = new();

    [Fact]
    public async Task Sends_client_notification_to_resolved_groups()
    {
        var evt = TestEvents.Valid(Audience.Topic("warranty.returns"), Audience.Branch("001"));
        var handler = CreateHandler(now: evt.OccurredAt.AddSeconds(10));

        await handler.Handle(evt, CancellationToken.None);

        var call = Assert.Single(_notifier.Calls);
        Assert.Equal(["ancona:topic:warranty.returns", "ancona:branch:001"], call.Groups.Select(g => g.Value));
        Assert.Equal(evt.EventId, call.Notification.EventId);
        Assert.Equal(evt.EventType, call.Notification.EventType);
        Assert.Equal(evt.OccurredAt, call.Notification.OccurredAt);
        Assert.Equal("k-1", call.Notification.Payload.GetProperty("warrantyReturnKey").GetString());
    }

    [Fact]
    public async Task Discards_event_older_than_max_age()
    {
        var evt = TestEvents.Valid();
        var handler = CreateHandler(now: evt.OccurredAt.AddSeconds(301));

        await handler.Handle(evt, CancellationToken.None);

        Assert.Empty(_notifier.Calls);
    }

    [Fact]
    public async Task Invalid_event_throws_and_sends_nothing()
    {
        var evt = TestEvents.Valid();
        evt.Audience = [];
        var handler = CreateHandler(now: evt.OccurredAt);

        await Assert.ThrowsAsync<InvalidEventDataException>(() => handler.Handle(evt, CancellationToken.None));
        Assert.Empty(_notifier.Calls);
    }

    private DispatchNotificationEventHandler CreateHandler(DateTimeOffset now) =>
        new(_notifier,
            Options.Create(new NotificationSetting { MaxEventAgeSeconds = 300 }),
            new FixedTimeProvider(now),
            NullLogger<DispatchNotificationEventHandler>.Instance);
}
