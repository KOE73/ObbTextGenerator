using SkiaSharp;

namespace ObbTextGenerator;

public sealed class NoiseBackgroundStageSettings : RenderStageSettingsBase
{
    /// <summary>
    /// Renamed to NoiseType to avoid conflict with the YAML discriminator 'type'.
    /// </summary>
    public NoiseType NoiseType { get; init; } = NoiseType.Fractal;
    
    public SampledValueSpec Freq { get; init; } = SampledValueSpec.Parse("0.005..0.04");
    
    public SampledValueSpec Octaves { get; init; } = SampledValueSpec.Parse("1..4");

    public SampledValueSpec Alpha { get; init; } = SampledValueSpec.Parse("0.2..0.6");

    /// <summary>
    /// Skia blend mode to use. Default is Multiply for dirt/shadows.
    /// </summary>
    public SKBlendMode BlendMode { get; init; } = SKBlendMode.Multiply;
}
