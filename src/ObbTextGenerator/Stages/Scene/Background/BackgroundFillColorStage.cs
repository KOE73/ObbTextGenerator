using SkiaSharp;

namespace ObbTextGenerator;

/// <summary>
/// A rendering stage that fills the entire background with a single solid color.
/// </summary>
/// <param name="color">The color to fill the background with.</param>
public sealed class BackgroundFillColorStage(
    BackgroundFillColorStageSettings settings,
    IColorProvider colorProvider) : RenderStageBase(settings)
{
    private readonly IColorProvider _colorProvider = colorProvider;

    /// <inheritdoc />
    public override string Name => "Background/FillColor";

    /// <inheritdoc />
    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        using var paint = new SKPaint();
        paint.Color = _colorProvider.GetColor(context);

        context.Canvas.DrawRect(window.Bounds, paint);
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return "background-fill";
    }
}
