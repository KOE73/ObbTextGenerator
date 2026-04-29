using SkiaSharp;

namespace ObbTextGenerator;

public sealed class PatternStageSettings : RenderStageSettingsBase
{
    public string? Group { get; init; }
    public string? PatternName { get; init; }

    public SKBlendMode BlendMode { get; init; } = SKBlendMode.SrcOver;
    public SampledValueSpec Alpha { get; init; } = SampledValueSpec.Parse("1");
}
