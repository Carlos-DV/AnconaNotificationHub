using System.Text.Json;
using AnconaNotificationHub.Contracts;

namespace Application.UnitTests.Contracts;

public class NotificationEventTests
{
    [Fact]
    public void Create_sets_event_id_and_utc_timestamp()
    {
        var before = DateTimeOffset.UtcNow;

        var evt = NotificationEvent.Create("warranty.return.status-changed", "ancona", "Tests",
            [Audience.Topic("warranty.returns")], new { Id = 1 });

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.InRange(evt.OccurredAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void Create_serializes_payload_in_camel_case()
    {
        var evt = NotificationEvent.Create("warranty.return.status-changed", "ancona", "Tests",
            [Audience.Topic("warranty.returns")], new { WarrantyReturnKey = "k-1", Status = "Pending" });

        Assert.Equal("k-1", evt.Payload.GetProperty("warrantyReturnKey").GetString());
        Assert.Equal("Pending", evt.Payload.GetProperty("status").GetString());
    }

    [Fact]
    public void Audience_factories_build_type_and_value()
    {
        Assert.Equal(("user", "38"), (Audience.User(38).Type, Audience.User(38).Value));
        Assert.Equal(("branch", "001"), (Audience.Branch("001").Type, Audience.Branch("001").Value));
        Assert.Equal(("entity", "warranty.return:abc"),
            (Audience.Entity("warranty.return", "abc").Type, Audience.Entity("warranty.return", "abc").Value));
        Assert.Equal(("all", ""), (Audience.All().Type, Audience.All().Value));
    }

    [Fact]
    public void Event_survives_default_json_round_trip_like_LilHermes()
    {
        var evt = NotificationEvent.Create("warranty.return.status-changed", "ancona", "Tests",
            [Audience.Branch("001"), Audience.Topic("warranty.returns")], new { Folio = "001001DE1" });

        var json = JsonSerializer.Serialize(evt);
        var back = JsonSerializer.Deserialize<NotificationEvent>(json)!;

        Assert.Equal(evt.EventId, back.EventId);
        Assert.Equal(evt.OccurredAt, back.OccurredAt);
        Assert.Equal(2, back.Audience.Count);
        Assert.Equal("001001DE1", back.Payload.GetProperty("folio").GetString());
    }
}
