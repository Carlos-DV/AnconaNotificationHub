using System.Text.Json;
using AnconaNotificationHub.Contracts;
using Application.Exceptions;
using Application.Features.DispatchEvent;
using Application.UnitTests.Support;

namespace Application.UnitTests.Features;

public class NotificationEventValidatorTests
{
    private const int DefaultMaxPayload = 32 * 1024;

    [Fact]
    public void Valid_event_returns_deduplicated_groups_in_order()
    {
        var evt = TestEvents.Valid(
            Audience.Topic("warranty.returns"),
            Audience.Topic("Warranty.Returns"),
            Audience.Branch("001"),
            Audience.All());

        var groups = NotificationEventValidator.ValidateAndResolveGroups(evt, DefaultMaxPayload);

        Assert.Equal(["ancona:topic:warranty.returns", "ancona:branch:001", "ancona:all"], groups.Select(g => g.Value));
    }

    [Fact]
    public void Empty_event_id_is_invalid() => AssertInvalid(e => e.EventId = Guid.Empty);

    [Theory]
    [InlineData("")]
    [InlineData("Warranty.Return")]
    [InlineData("warranty return")]
    public void Bad_event_type_is_invalid(string eventType) => AssertInvalid(e => e.EventType = eventType);

    [Theory]
    [InlineData("")]
    [InlineData("an:cona")]
    public void Bad_tenant_is_invalid(string tenant) => AssertInvalid(e => e.Tenant = tenant);

    [Fact]
    public void Empty_audience_is_invalid() => AssertInvalid(e => e.Audience = []);

    [Fact]
    public void Unknown_audience_type_is_invalid() =>
        AssertInvalid(e => e.Audience = [new Audience { Type = "role", Value = "admin" }]);

    [Fact]
    public void Audience_value_with_spaces_is_invalid() =>
        AssertInvalid(e => e.Audience = [Audience.Topic("warranty returns")]);

    [Fact]
    public void Non_object_payload_is_invalid() =>
        AssertInvalid(e => e.Payload = JsonSerializer.SerializeToElement(5));

    [Fact]
    public void Missing_payload_is_invalid() => AssertInvalid(e => e.Payload = default);

    [Fact]
    public void Payload_over_limit_is_invalid() => AssertInvalid(_ => { }, maxPayloadBytes: 10);

    private static void AssertInvalid(Action<NotificationEvent> mutate, int maxPayloadBytes = DefaultMaxPayload)
    {
        var evt = TestEvents.Valid();
        mutate(evt);
        Assert.Throws<InvalidEventDataException>(() =>
            NotificationEventValidator.ValidateAndResolveGroups(evt, maxPayloadBytes));
    }
}
