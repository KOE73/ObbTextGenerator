using SkiaSharp;

namespace ObbTextGenerator;

public sealed class WriteDebugPreviewStage(
    WriteDebugPreviewStageSettings settings,
    string outputRoot) : IPipelineStage
{
    private readonly WriteDebugPreviewStageSettings _settings = settings;
    private readonly string _outputRoot = outputRoot;

    public string Name => $"Write/DebugPreview ({_settings.Path})";

    public void Apply(RenderContext context)
    {
        var fullOutputDirectory = Path.Combine(_outputRoot, context.SetName, _settings.Path);
        Directory.CreateDirectory(fullOutputDirectory);

        var encodedFormat = ResolveEncodedFormat();
        var extension = ImageEncodingResolver.ResolveExtension(encodedFormat);
        var fileName = $"{context.SampleIndex:D6}.{extension}";
        var filePath = Path.Combine(fullOutputDirectory, fileName);

        using var previewBitmap = BuildPreviewBitmap(context);
        using var image = SKImage.FromBitmap(previewBitmap);
        using var data = image.Encode(encodedFormat, _settings.Quality);
        if (data == null)
        {
            return;
        }

        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);

        context.AddTrace($"debug-preview: {_settings.Path}", $"file={fileName}");
    }

    private SKBitmap BuildPreviewBitmap(RenderContext context)
    {
        var finalWidth = context.Width + _settings.PanelWidth;
        var finalHeight = context.Height;

        var bitmap = new SKBitmap(finalWidth, finalHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        using var image = SKImage.FromBitmap(context.Bitmap);
        canvas.DrawImage(image, 0, 0);

        foreach (var overlayLayer in _settings.OverlayLayers)
        {
            AnnotationOverlayRenderer.DrawLayer(canvas, context, overlayLayer);
        }

        DrawPanel(canvas, context);
        return bitmap;
    }

    private void DrawPanel(SKCanvas canvas, RenderContext context)
    {
        var panelLeft = context.Width;
        var padding = _settings.Padding;
        var contentWidth = _settings.PanelWidth - padding * 2;

        using var separatorPaint = new SKPaint();
        separatorPaint.Color = new SKColor(220, 220, 220);
        separatorPaint.StrokeWidth = 1;
        canvas.DrawLine(panelLeft, 0, panelLeft, context.Height, separatorPaint);

        using var titlePaint = new SKPaint();
        titlePaint.IsAntialias = true;
        titlePaint.Color = SKColors.Black;
        titlePaint.Style = SKPaintStyle.Fill;

        using var titleFont = new SKFont(SKTypeface.Default, _settings.FontSize + 2);
        canvas.DrawText(_settings.Title, panelLeft + padding, padding + titleFont.Size, titleFont, titlePaint);

        using var textPaint = new SKPaint();
        textPaint.IsAntialias = true;
        textPaint.Color = SKColors.Black;

        using var textFont = new SKFont(SKTypeface.Default, _settings.FontSize);
        var lineHeight = textFont.Size + 6;
        var currentY = padding + titleFont.Size + 18;

        var lines = BuildTraceLines(context);
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            if (currentY + lineHeight > context.Height - padding)
            {
                DrawEllipsis(canvas, panelLeft + padding, currentY, textFont, textPaint);
                break;
            }

            var line = lines[lineIndex];
            var wrappedSegments = WrapLine(line, textPaint, textFont, contentWidth);
            foreach (var segment in wrappedSegments)
            {
                if (currentY + lineHeight > context.Height - padding)
                {
                    DrawEllipsis(canvas, panelLeft + padding, currentY, textFont, textPaint);
                    return;
                }

                canvas.DrawText(segment, panelLeft + padding, currentY, textFont, textPaint);
                currentY += lineHeight;
            }
        }
    }

    private List<string> BuildTraceLines(RenderContext context)
    {
        var lines = new List<string>();
        foreach (var entry in context.TraceEntries)
        {
            lines.Add(entry.GetIndentedText(_settings.TraceVerbosity, _settings.TraceIndentText));
        }

        return lines;
    }

    private static List<string> WrapLine(string line, SKPaint paint, SKFont font, float maxWidth)
    {
        var segments = new List<string>();
        if (string.IsNullOrEmpty(line))
        {
            segments.Add(string.Empty);
            return segments;
        }

        var indentLength = 0;
        while (indentLength < line.Length && char.IsWhiteSpace(line[indentLength]))
        {
            indentLength++;
        }

        var indentPrefix = line[..indentLength];
        var content = line[indentLength..];
        if (string.IsNullOrWhiteSpace(content))
        {
            segments.Add(line);
            return segments;
        }

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var current = indentPrefix;

        foreach (var word in words)
        {
            var candidate = current.Length == indentPrefix.Length ? $"{indentPrefix}{word}" : $"{current} {word}";
            var candidateWidth = font.MeasureText(candidate, paint);
            if (candidateWidth <= maxWidth || current.Length == indentPrefix.Length)
            {
                current = candidate;
                continue;
            }

            segments.Add(current);
            current = $"{indentPrefix}{word}";
        }

        if (current.Length > indentPrefix.Length)
        {
            segments.Add(current);
        }

        return segments;
    }

    private static void DrawEllipsis(SKCanvas canvas, float x, float y, SKFont font, SKPaint paint)
    {
        canvas.DrawText("...", x, y, font, paint);
    }

    private SKEncodedImageFormat ResolveEncodedFormat()
    {
        return ImageEncodingResolver.ResolveFormat(_settings.Format);
    }
}
