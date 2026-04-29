using SkiaSharp;

namespace ObbTextGenerator;

public sealed class SpotlightStage(SpotlightStageSettings settings) : RenderStageBase(settings)
{
    private readonly SpotlightStageSettings _settings = settings;

    public override string Name => "Render/Spotlight";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;
        var center = new SKPoint(
            (float)(random.NextDouble() * window.Width + window.Left),
            (float)(random.NextDouble() * window.Height + window.Top));
        float baseSize = Math.Max(window.Width, window.Height);
        float radius = _settings.Radius.Sample(random, baseSize);
        float alpha = _settings.Alpha.Sample(random);

        using var paint = new SKPaint
        {
            Shader = SKShader.CreateRadialGradient(
                center,
                radius,
                [SKColors.White.WithAlpha((byte)(alpha * 255)), SKColors.Transparent],
                null,
                SKShaderTileMode.Clamp),
            BlendMode = _settings.BlendMode
        };

        context.Canvas.DrawRect(window.Bounds, paint);
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "spotlight";
    }
}

public sealed class SpotlightStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(SpotlightStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new SpotlightStage((SpotlightStageSettings)settings);
    }
}
