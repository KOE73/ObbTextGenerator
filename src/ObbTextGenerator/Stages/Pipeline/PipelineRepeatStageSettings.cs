namespace ObbTextGenerator;

public sealed class PipelineRepeatStageSettings : PipelineCompositeStageSettingsBase
{
    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public List<StageSettingsBase> Stages { get; init; } = new();
}
