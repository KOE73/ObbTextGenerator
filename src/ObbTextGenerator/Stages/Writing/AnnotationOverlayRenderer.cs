using SkiaSharp;

namespace ObbTextGenerator;

public static class AnnotationOverlayRenderer
{
    public static void DrawLayer(SKCanvas canvas, RenderContext context, AnnotationOverlayLayerSettings settings)
    {
        if (!context.AnnotationLayers.TryGetValue(settings.LayerName, out var layer))
        {
            return;
        }

        var color = ResolveColor(context, settings.ColorRole);

        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = color;
        paint.StrokeWidth = settings.StrokeWidth;
        paint.Style = settings.Fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke;

        using var textPaint = new SKPaint();
        textPaint.IsAntialias = true;
        textPaint.Color = color;

        using var font = new SKFont(SKTypeface.Default, 14);

        foreach (var annotation in layer.Annotations)
        {
            if (annotation.Points.Length < 2)
            {
                continue;
            }

            using var path = new SKPath();
            path.MoveTo(annotation.Points[0]);
            for (int pointIndex = 1; pointIndex < annotation.Points.Length; pointIndex++)
            {
                path.LineTo(annotation.Points[pointIndex]);
            }

            path.Close();
            canvas.DrawPath(path, paint);

            if (settings.ShowText && !string.IsNullOrEmpty(annotation.Text))
            {
                canvas.DrawText(annotation.Text, annotation.Points[0].X, annotation.Points[0].Y - 5, font, textPaint);
            }
        }
    }

    private static SKColor ResolveColor(RenderContext context, string colorRole)
    {
        var activeScheme = context.ActiveScheme;
        if (activeScheme == null)
        {
            return SKColors.Magenta;
        }

        if (!activeScheme.Roles.TryGetValue(colorRole, out var roleSettings))
        {
            return SKColors.Magenta;
        }

        return roleSettings switch
        {
            ConstantColorProviderSettings constantSettings => new ConstantColorProvider(constantSettings).GetColor(context),
            RandomColorProviderSettings randomSettings => new RandomColorProvider(randomSettings).GetColor(context),
            GrayColorProviderSettings graySettings => new GrayColorProvider(graySettings).GetColor(context),
            _ => SKColors.Magenta
        };
    }
}
