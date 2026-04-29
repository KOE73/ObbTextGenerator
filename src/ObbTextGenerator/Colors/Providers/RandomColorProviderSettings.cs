namespace ObbTextGenerator;

public sealed class RandomColorProviderSettings : ColorProviderSettingsBase
{
    /// <summary>
    /// Preset name: "any", "dark", "light". Only used if R, G, B are not specified.
    /// </summary>
    public string Preset { get; init; } = "any";
    
    public object? R { get; init; }
    public object? G { get; init; }
    public object? B { get; init; }
    public object? A { get; init; }
}
