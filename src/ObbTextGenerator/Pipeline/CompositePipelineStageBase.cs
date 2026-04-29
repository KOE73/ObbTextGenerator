namespace ObbTextGenerator;

public abstract class CompositePipelineStageBase(PipelineCompositeStageSettingsBase settings) : IPipelineStage
{
    private readonly PipelineCompositeStageSettingsBase _settings = settings;

    public abstract string Name { get; }

    public void Apply(RenderContext context)
    {
        using var traceScope = context.BeginTraceScope(BuildTraceSummary(context), BuildTraceDetails(context));

        if (_settings.Window == null)
        {
            ApplyCore(context);
            return;
        }

        var windows = RenderWindowResolver.Resolve(_settings.Window, context);

        foreach (var window in windows)
        {
            context.PushRenderWindow(window);

            try
            {
                ApplyCore(context);
            }
            finally
            {
                context.PopRenderWindow();
            }
        }
    }

    protected abstract void ApplyCore(RenderContext context);

    protected virtual string BuildTraceSummary(RenderContext context)
    {
        return Name;
    }

    protected virtual string? BuildTraceDetails(RenderContext context)
    {
        return null;
    }
}
