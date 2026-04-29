using SkiaSharp;

namespace ObbTextGenerator;

public enum NoiseType
{
    Fractal,
    Turbulence
}

public sealed class NoiseBackgroundStage(NoiseBackgroundStageSettings settings) : RenderStageBase(settings)
{
    private readonly NoiseBackgroundStageSettings _settings = settings;

    public override string Name => "Background/Noise";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;
        using var paint = new SKPaint();

        float freqX = _settings.Freq.Sample(random);
        float freqY = _settings.Freq.Sample(random);
        int octaves = _settings.Octaves.SampleInt(random);
        float alpha = _settings.Alpha.Sample(random);

        if (_settings.NoiseType == NoiseType.Fractal)
        {
            paint.Shader = SKShader.CreatePerlinNoiseFractalNoise(freqX, freqY, octaves, random.Next());
        }
        else
        {
            paint.Shader = SKShader.CreatePerlinNoiseTurbulence(freqX, freqY, octaves, random.Next());
        }

        paint.BlendMode = _settings.BlendMode;
        paint.Color = SKColors.White.WithAlpha((byte)(alpha * 255));

        context.Canvas.DrawRect(window.Bounds, paint);
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "noise-background";
    }
}

public sealed class NoiseBackgroundStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(NoiseBackgroundStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new NoiseBackgroundStage((NoiseBackgroundStageSettings)settings);
    }
}
