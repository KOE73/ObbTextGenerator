namespace ObbTextGenerator;

/// <summary>
/// Defines a named set of color roles for semantic color resolution.
/// </summary>
public record ColorSchemeSettings
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, ColorProviderSettingsBase> Roles { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
