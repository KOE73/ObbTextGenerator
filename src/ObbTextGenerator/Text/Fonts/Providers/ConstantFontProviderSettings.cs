using SkiaSharp;

namespace ObbTextGenerator;

public sealed class ConstantFontProviderSettings : FontProviderSettingsBase
{
    public string Family { get; init; } = "Arial";
    public SKFontStyleWeight Weight { get; init; } = SKFontStyleWeight.Normal;
    public SKFontStyleWidth Width { get; init; } = SKFontStyleWidth.Normal;
    public SKFontStyleSlant Slant { get; init; } = SKFontStyleSlant.Upright;
    public float Size { get; init; } = 24;
}
