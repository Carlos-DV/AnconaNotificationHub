using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Domain;

/// <summary>
/// Nombre de un grupo de SignalR: "{tenant}:{type}:{value}", o "{tenant}:all".
/// Todo en minúsculas; el tenant va siempre al inicio para que ningún evento cruce de empresa.
/// </summary>
public sealed partial record GroupName
{
    public const int MaxValueLength = 200;
    private const string AllType = "all";

    public string Value { get; }

    private GroupName(string value) => Value = value;

    public static bool TryCreate(string? tenant, string? type, string? value, [NotNullWhen(true)] out GroupName? group)
    {
        group = null;
        var normalizedTenant = Normalize(tenant);
        var normalizedType = Normalize(type);

        if (!TenantPattern().IsMatch(normalizedTenant) || !TypePattern().IsMatch(normalizedType))
            return false;

        if (normalizedType == AllType)
        {
            group = new GroupName($"{normalizedTenant}:{AllType}");
            return true;
        }

        var normalizedValue = Normalize(value);
        if (normalizedValue.Length > MaxValueLength || !ValuePattern().IsMatch(normalizedValue))
            return false;

        group = new GroupName($"{normalizedTenant}:{normalizedType}:{normalizedValue}");
        return true;
    }

    public override string ToString() => Value;

    private static string Normalize(string? part) => (part ?? string.Empty).Trim().ToLowerInvariant();

    [GeneratedRegex("^[a-z0-9._-]+$")]
    private static partial Regex TenantPattern();

    [GeneratedRegex("^[a-z]+$")]
    private static partial Regex TypePattern();

    [GeneratedRegex("^[a-z0-9._:-]+$")]
    private static partial Regex ValuePattern();
}
