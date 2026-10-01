using AnconaNotificationHub.Contracts;

namespace Application.UnitTests.Support;

internal static class TestEvents
{
    public static NotificationEvent Valid(params Audience[] audience) =>
        NotificationEvent.Create(
            "warranty.return.status-changed",
            "ancona",
            "Tests",
            audience.Length > 0 ? audience : new[] { Audience.Topic("warranty.returns") },
            new { WarrantyReturnKey = "k-1", Status = "Pending" });
}
