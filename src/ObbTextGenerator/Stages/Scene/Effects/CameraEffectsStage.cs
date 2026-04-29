using SkiaSharp;

namespace ObbTextGenerator;

public sealed class CameraEffectsStage(CameraEffectsStageSettings settings) : RenderStageBase(settings)
{
    private readonly CameraEffectsStageSettings _settings = settings;

    public override string Name => "PostProcess/CameraEffects";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;
        float blurSigma = _settings.Blur.Sample(random);

        using var paint = new SKPaint();
        
        if (blurSigma > 0.01f)
        {
            paint.ImageFilter = SKImageFilter.CreateBlur(blurSigma, blurSigma);
        }

        if (_settings.GrainAlpha > 0.01f)
        {
            // Simple grain using a transparent gray layer with Plus blend mode
            paint.ColorFilter = SKColorFilter.CreateBlendMode(
                SKColors.Gray.WithAlpha((byte)(_settings.GrainAlpha * 255)),
                SKBlendMode.Plus);
        }

        if (paint.ImageFilter == null && paint.ColorFilter == null)
        {
            return;
        }

        using var snapshot = SKImage.FromBitmap(context.Bitmap);
        context.Canvas.DrawImage(snapshot, 0, 0, paint);
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "camera-effects";
    }
}

public sealed class CameraEffectsStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(CameraEffectsStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new CameraEffectsStage((CameraEffectsStageSettings)settings);
    }
}
