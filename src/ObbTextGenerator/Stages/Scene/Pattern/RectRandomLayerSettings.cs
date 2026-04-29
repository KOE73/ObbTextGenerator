namespace ObbTextGenerator;

public sealed class RectRandomLayerSettings : PatternLayerSettingsBase
{
    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public SampledValueSpec Width { get; init; } = SampledValueSpec.Parse("0.1..0.2");

    public SampledValueSpec Height { get; init; } = SampledValueSpec.Parse("0.1..0.2");

    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0");
}
