namespace ObbTextGenerator;

public sealed class PipelineBlockStage(PipelineBlockStageSettings settings, List<IPipelineStage> stages) : CompositePipelineStageBase(settings)
{
    private readonly List<IPipelineStage> _stages = stages;

    public override string Name => "Pipeline/Block";

    protected override void ApplyCore(RenderContext context)
    {
        foreach (var stage in _stages)
        {
            stage.Apply(context);
        }
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "block";
    }
}
