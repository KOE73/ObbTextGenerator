namespace ObbTextGenerator;

public sealed class PipelineProgramDefinition
{
    public required string Name { get; init; }

    public string Group { get; init; } = "default";

    public double Weight { get; init; } = 1.0;

    public List<StageSettingsBase> Stages { get; init; } = new();
}
