using SkiaSharp;

namespace ObbTextGenerator;

public sealed class ConstantFontProvider : IFontProvider
{
    private readonly SKTypeface _typeface;
    private readonly float _size;

    public ConstantFontProvider(ConstantFontProviderSettings settings)
    {
        _typeface = SKTypeface.FromFamilyName(settings.Family, (int)settings.Weight, (int)settings.Width, settings.Slant);
        _size = settings.Size;
    }

    public SKTypeface GetTypeface(RenderContext context) => _typeface;
    public float GetSize(RenderContext context) => _size;
}
