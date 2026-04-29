namespace ObbTextGenerator;

public sealed class AnnotationOverlayStageSettings : RenderStageSettingsBase
{
    /// <summary>
    /// Which layer to visualize.
    /// </summary>
    public string LayerName { get; init; } = "collision";

    /// <summary>
    /// Color role to use from the current scheme (e.g. "overlay", "text").
    /// </summary>
    public string ColorRole { get; init; } = "overlay";

    public float StrokeWidth { get; init; } = 2.0f;
    
    /// <summary>
    /// If true, draws as a solid filled shape. If false, draws as an outline.
    /// </summary>
    public bool Fill { get; init; } = false;

    /// <summary>
    /// If true, draws the text content near the annotation.
    /// </summary>
    public bool ShowText { get; init; } = false;
}
