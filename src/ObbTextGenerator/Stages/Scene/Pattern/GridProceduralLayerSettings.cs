namespace ObbTextGenerator;

public sealed class GridProceduralLayerSettings : PatternLayerSettingsBase
{
    public float SpacingX { get; init; } = 0.5f;
    public float SpacingY { get; init; } = 0.5f;
    public SampledValueSpec Thickness { get; init; } = SampledValueSpec.Parse("1");
    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0");
    public float PositionJitter { get; init; } = 0f;
}
