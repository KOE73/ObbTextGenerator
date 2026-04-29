namespace ObbTextGenerator;

public sealed class PipelineProgramStageSettings : PipelineCompositeStageSettingsBase
{
    public required string ProgramName { get; init; }

    public PipelineProgramRenderMode RenderMode { get; init; } = PipelineProgramRenderMode.Direct;

    public PipelineProgramSurfaceSettings? Surface { get; init; }

    public PipelineProgramPlaceSettings? Place { get; init; }
}
