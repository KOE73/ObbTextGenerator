using SkiaSharp;

namespace ObbTextGenerator;

public abstract class PatternLayerSettingsBase
{
    public ColorProviderSettingsBase? Color { get; init; }
    public SKBlendMode BlendMode { get; init; } = SKBlendMode.SrcOver;
    public SampledValueSpec Alpha { get; init; } = SampledValueSpec.Parse("1");
}
