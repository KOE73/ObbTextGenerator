namespace ObbTextGenerator;

public sealed class WriteDebugPreviewStageSettings : StageSettingsBase
{
    public string Path { get; init; } = "debug_preview";

    public string Format { get; init; } = "png";

    public int Quality { get; init; } = 100;

    public int PanelWidth { get; init; } = 380;

    public int Padding { get; init; } = 16;

    public int FontSize { get; init; } = 16;

    public string Title { get; init; } = "Trace";

    public string TraceIndentText { get; init; } = "  ";

    public RenderTraceVerbosity TraceVerbosity { get; init; } = RenderTraceVerbosity.Compact;

    public List<AnnotationOverlayLayerSettings> OverlayLayers { get; init; } = new();
}
