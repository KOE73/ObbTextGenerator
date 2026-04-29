using SkiaSharp;

namespace ObbTextGenerator;

public sealed class MeasuredTextBlock
{
    public required IReadOnlyList<TextBlockLineLayout> Lines { get; init; }

    public SKRect TightBounds { get; init; }

    public SKRect FontMetricsBounds { get; init; }

    public SKRect CapHeightBounds { get; init; }

    public SKRect XHeightBounds { get; init; }

    public float TotalWidth { get; init; }
}
