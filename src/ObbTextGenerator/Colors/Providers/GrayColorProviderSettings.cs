namespace ObbTextGenerator;

public sealed class GrayColorProviderSettings : ColorProviderSettingsBase
{
    /// <summary>
    /// Intensity value or range [min, max] (0-255).
    /// </summary>
    public object? Intensity { get; init; } = 128;

    /// <summary>
    /// Alpha value or range [min, max] (0-255).
    /// </summary>
    public object? Alpha { get; init; } = 255;
}
