namespace ObbTextGenerator;

public sealed class PipelineSelectStageSettings : PipelineCompositeStageSettingsBase
{
    public string? Group { get; init; }

    public PipelineSelectionMode Mode { get; init; } = PipelineSelectionMode.Single;

    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public bool AllowDuplicates { get; init; }

    public double NoneWeight { get; init; } = 1.0;
}
