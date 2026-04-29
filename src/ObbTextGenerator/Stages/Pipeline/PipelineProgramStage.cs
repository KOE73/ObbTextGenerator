namespace ObbTextGenerator;

public sealed class PipelineProgramStage(
    PipelineProgramStageSettings settings,
    string programName,
    List<IPipelineStage> stages) : CompositePipelineStageBase(settings)
{
    private readonly PipelineProgramStageSettings _settings = settings;
    private readonly string _programName = programName;
    private readonly List<IPipelineStage> _stages = stages;

    public override string Name => $"Pipeline/Program({_programName})";

    protected override void ApplyCore(RenderContext context)
    {
        if (_settings.RenderMode == PipelineProgramRenderMode.Surface)
        {
            var surfaceSettings = _settings.Surface ?? new PipelineProgramSurfaceSettings();
            var placeSettings = _settings.Place ?? throw new InvalidOperationException($"Pipeline program '{_programName}' requires 'place' settings in Surface mode.");
            context.Session.ExecuteSurfaceProgram(context, _programName, _stages, surfaceSettings, placeSettings);
            return;
        }

        foreach (var stage in _stages)
        {
            stage.Apply(context);
        }
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return _settings.RenderMode == PipelineProgramRenderMode.Surface
            ? $"program-surface: {_programName}"
            : $"program: {_programName}";
    }
}
