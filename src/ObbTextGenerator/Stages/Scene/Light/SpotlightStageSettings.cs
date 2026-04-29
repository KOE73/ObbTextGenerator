using SkiaSharp;

namespace ObbTextGenerator;

public sealed class SpotlightStageSettings : RenderStageSettingsBase
{
    public SampledValueSpec Radius { get; init; } = SampledValueSpec.Parse("25%..100%");

    public SampledValueSpec Alpha { get; init; } = SampledValueSpec.Parse("0.3..0.8");

    /// <summary>
    /// Blend mode for lighting. Screen or SoftLight are common.
    /// </summary>
    public SKBlendMode BlendMode { get; init; } = SKBlendMode.Screen;
}
