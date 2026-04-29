using SkiaSharp;

namespace ObbTextGenerator;

public sealed class PipelineProgramPlaceSettings : RenderWindowSettings
{
    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0");

    public float Opacity { get; init; } = 1f;

    public SKBlendMode BlendMode { get; init; } = SKBlendMode.SrcOver;
}
