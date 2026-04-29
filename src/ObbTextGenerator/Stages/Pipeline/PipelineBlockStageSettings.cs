namespace ObbTextGenerator;

public sealed class PipelineBlockStageSettings : PipelineCompositeStageSettingsBase
{
    public List<StageSettingsBase> Stages { get; init; } = new();
}
