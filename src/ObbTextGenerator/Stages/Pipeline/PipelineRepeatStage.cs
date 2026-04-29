namespace ObbTextGenerator;

public sealed class PipelineRepeatStage(PipelineRepeatStageSettings settings, List<IPipelineStage> stages) : CompositePipelineStageBase(settings)
{
    private readonly PipelineRepeatStageSettings _settings = settings;
    private readonly List<IPipelineStage> _stages = stages;

    public override string Name => "Pipeline/Repeat";

    protected override void ApplyCore(RenderContext context)
    {
        var count = _settings.Count.SampleInt(context.Settings.Random);
        context.AddTrace("repeat-count", count.ToString());

        for (int repeatIndex = 0; repeatIndex < count; repeatIndex++)
        {
            foreach (var stage in _stages)
            {
                stage.Apply(context);
            }
        }
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "repeat";
    }
}
