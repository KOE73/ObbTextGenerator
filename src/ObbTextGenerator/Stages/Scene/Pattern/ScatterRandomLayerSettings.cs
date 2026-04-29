namespace ObbTextGenerator;

public sealed class ScatterRandomLayerSettings : PatternLayerSettingsBase
{
    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("10..20");

    public SampledValueSpec Width { get; init; } = SampledValueSpec.Parse("0.05..0.1");

    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0..360");
}
