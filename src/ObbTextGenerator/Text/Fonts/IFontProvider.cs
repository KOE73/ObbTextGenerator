using SkiaSharp;

namespace ObbTextGenerator;

public interface IFontProvider
{
    SKTypeface GetTypeface(RenderContext context);
    float GetSize(RenderContext context);
}
