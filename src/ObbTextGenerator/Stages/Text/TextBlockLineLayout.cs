using SkiaSharp;

namespace ObbTextGenerator;

public sealed class TextBlockLineLayout
{
    public required string Text { get; init; }

    public float OffsetX { get; init; }

    public float BaselineY { get; init; }

    public float Width { get; init; }

    public SKRect TightBounds { get; init; }

    public SKRect FontMetricsBounds { get; init; }

    public SKRect CapHeightBounds { get; init; }

    public SKRect XHeightBounds { get; init; }

    public bool IsJustified { get; init; }

    public float AdditionalSpaceWidth { get; init; }
}
