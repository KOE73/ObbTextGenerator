namespace ObbTextGenerator;

public sealed class TiledPatternStageSettings
{
    /// <summary>
    /// Tile size in pixels or percentage (e.g. "50", "15%").
    /// </summary>
    public SampledValueSpec TileSize { get; init; } = SampledValueSpec.Parse("50..100");

    public List<PatternLayerSettingsBase> Layers { get; init; } = new();

    /// <summary>
    /// Global rotation of the pattern.
    /// </summary>
    public SampledValueSpec GlobalRotation { get; init; } = SampledValueSpec.Parse("-10..10");
}
