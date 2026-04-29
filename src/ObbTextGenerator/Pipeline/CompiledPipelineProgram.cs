namespace ObbTextGenerator;

public sealed class CompiledPipelineProgram
{
    public required string Name { get; init; }

    public string Group { get; init; } = "default";

    public double Weight { get; init; } = 1.0;

    public required List<IPipelineStage> Stages { get; init; }
}
