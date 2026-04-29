using SkiaSharp;

namespace ObbTextGenerator;

public abstract class RenderStageBase(RenderStageSettingsBase settings) : IPipelineStage
{
    private readonly RenderStageSettingsBase _settings = settings;

    public abstract string Name { get; }

    public void Apply(RenderContext context)
    {
        context.AddTrace(BuildTraceSummary(context), BuildTraceDetails(context));

        var windows = RenderWindowResolver.Resolve(_settings.Window, context);

        foreach (var window in windows)
        {
            context.Canvas.Save();

            using var clipPath = window.CreateClipPath();
            context.Canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

            context.PushRenderWindow(window);

            try
            {
                ApplyCore(context, window);
            }
            finally
            {
                context.PopRenderWindow();
                context.Canvas.Restore();
            }
        }
    }

    protected abstract void ApplyCore(RenderContext context, RenderWindow window);

    protected virtual string BuildTraceSummary(RenderContext context)
    {
        return Name;
    }

    protected virtual string? BuildTraceDetails(RenderContext context)
    {
        return null;
    }
}
