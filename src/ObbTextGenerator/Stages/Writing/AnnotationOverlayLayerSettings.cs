namespace ObbTextGenerator;

public sealed class AnnotationOverlayLayerSettings
{
    public required string LayerName { get; init; }

    public string ColorRole { get; init; } = "overlay";

    public float StrokeWidth { get; init; } = 2.0f;

    public bool Fill { get; init; }

    public bool ShowText { get; init; }
}
