using SkiaSharp;

namespace ObbTextGenerator;

public sealed class AnnotationOverlayStage(AnnotationOverlayStageSettings settings) : RenderStageBase(settings)
{
    private readonly AnnotationOverlayStageSettings _settings = settings;

    public override string Name => $"Visualization/AnnotationOverlay({_settings.LayerName})";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        AnnotationOverlayRenderer.DrawLayer(context.Canvas, context, new AnnotationOverlayLayerSettings
        {
            LayerName = _settings.LayerName,
            ColorRole = _settings.ColorRole,
            StrokeWidth = _settings.StrokeWidth,
            Fill = _settings.Fill,
            ShowText = _settings.ShowText
        });
    }

    protected override string? BuildTraceDetails(RenderContext context)
    {
        return $"layer={_settings.LayerName}";
    }
}

public sealed class AnnotationOverlayStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(AnnotationOverlayStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new AnnotationOverlayStage((AnnotationOverlayStageSettings)settings);
    }
}
