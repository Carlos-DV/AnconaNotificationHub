using Application.Abstractions.Identity;

namespace Application.UnitTests.Support;

internal sealed class FakeSubscriptionPolicy(bool allowed) : ISubscriptionPolicy
{
    public bool IsAllowed(string tenant, string type, string value) => allowed;
}
