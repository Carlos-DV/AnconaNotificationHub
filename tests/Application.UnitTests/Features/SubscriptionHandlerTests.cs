using Application.Exceptions;
using Application.Features.Connections;
using Application.Settings;
using Application.UnitTests.Support;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Features;

public class SubscriptionHandlerTests
{
    [Theory]
    [InlineData("topic", "warranty.returns", "ancona:topic:warranty.returns")]
    [InlineData("ENTITY", "Warranty.Return:ABC", "ancona:entity:warranty.return:abc")]
    public void Accepts_topic_and_entity(string type, string value, string expected)
    {
        var group = CreateHandler().ValidateSubscribe("ancona", type, value, []);
        Assert.Equal(expected, group.Value);
    }

    [Theory]
    [InlineData("user", "99")]
    [InlineData("branch", "001")]
    [InlineData("perm", "warranty.view")]
    [InlineData("all", "")]
    [InlineData("topic", "warranty returns")]
    [InlineData(null, "warranty.returns")]
    public void Rejects_identity_types_and_invalid_values(string? type, string value) =>
        Assert.Throws<SubscriptionRejectedException>(() => CreateHandler().ValidateSubscribe("ancona", type, value, []));

    [Fact]
    public void Rejects_when_limit_reached()
    {
        var handler = CreateHandler(maxSubscriptions: 2);
        Assert.Throws<SubscriptionRejectedException>(() =>
            handler.ValidateSubscribe("ancona", "topic", "c", ["ancona:topic:a", "ancona:topic:b"]));
    }

    [Fact]
    public void Already_subscribed_group_is_accepted_even_at_limit()
    {
        var handler = CreateHandler(maxSubscriptions: 2);
        var group = handler.ValidateSubscribe("ancona", "topic", "a", ["ancona:topic:a", "ancona:topic:b"]);
        Assert.Equal("ancona:topic:a", group.Value);
    }

    [Fact]
    public void Rejects_when_policy_denies() =>
        Assert.Throws<SubscriptionRejectedException>(() =>
            CreateHandler(allowed: false).ValidateSubscribe("ancona", "topic", "warranty.returns", []));

    [Fact]
    public void Unsubscribe_builds_only_dynamic_groups()
    {
        var handler = CreateHandler();
        Assert.Equal("ancona:topic:warranty.returns", handler.TryBuildUnsubscribe("ancona", "topic", "warranty.returns")?.Value);
        Assert.Null(handler.TryBuildUnsubscribe("ancona", "user", "38"));
    }

    private static SubscriptionHandler CreateHandler(int maxSubscriptions = 50, bool allowed = true) =>
        new(new FakeSubscriptionPolicy(allowed),
            Options.Create(new NotificationSetting { MaxSubscriptionsPerConnection = maxSubscriptions }));
}
