namespace Application.Exceptions;

/// <summary>Sobre mal formado. LilHermes lo reintenta y termina en la DLQ.</summary>
public sealed class InvalidEventDataException(string message) : Exception(message);
