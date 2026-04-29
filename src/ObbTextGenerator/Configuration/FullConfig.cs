namespace ObbTextGenerator;

/// <summary>
/// Root configuration object containing all generator settings.
/// </summary>
public sealed class FullConfig
{
    public GenerationSettings General { get; init; } = new();

    public List<StageSettingsBase> Stages { get; init; } = new();

    public List<PipelineProgramDefinition> PipelinePrograms { get; init; } = new();

    public List<ColorSchemeSettings> Schemes { get; init; } = new();

    public List<NamedPatternSettings> Patterns { get; init; } = new();
}
