namespace ObbTextGenerator;

public sealed class LineRandomLayerSettings : PatternLayerSettingsBase
{
    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public SampledValueSpec Thickness { get; init; } = SampledValueSpec.Parse("1");
}
