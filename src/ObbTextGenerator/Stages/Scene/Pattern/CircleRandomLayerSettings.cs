namespace ObbTextGenerator;

public sealed class CircleRandomLayerSettings : PatternLayerSettingsBase
{
    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public SampledValueSpec Width { get; init; } = SampledValueSpec.Parse("0.1..0.2");
}
