namespace ObbTextGenerator;

public sealed class NamedPatternSettings
{
    public required string Name { get; init; }
    public string Group { get; init; } = "default";
    public required TiledPatternStageSettings Pattern { get; init; }
}
