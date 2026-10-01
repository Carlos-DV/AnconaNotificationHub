namespace Application.Exceptions;

/// <summary>Subscribe no permitido; el hub lo convierte en HubException.</summary>
public sealed class SubscriptionRejectedException(string message) : Exception(message);
