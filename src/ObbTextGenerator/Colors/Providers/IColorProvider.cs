using SkiaSharp;

namespace ObbTextGenerator;

public interface IColorProvider
{
    SKColor GetColor(RenderContext context);
}
