namespace ObbTextGenerator;

public sealed class PerlinProceduralLayerSettings : PatternLayerSettingsBase
{
    public float BaseFrequencyX { get; init; } = 0.01f;
    public float BaseFrequencyY { get; init; } = 0.01f;
    public int Octaves { get; init; } = 1;
}
