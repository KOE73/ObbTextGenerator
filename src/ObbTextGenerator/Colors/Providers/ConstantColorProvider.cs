using SkiaSharp;

namespace ObbTextGenerator;

public sealed class ConstantColorProvider : IColorProvider
{
    private readonly SKColor _color;

    public ConstantColorProvider(ConstantColorProviderSettings settings)
    {
        _color = SKColor.Parse(settings.Color);
    }

    public SKColor GetColor(RenderContext context) => _color;
}
