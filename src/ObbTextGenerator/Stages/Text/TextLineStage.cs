using SkiaSharp;
using System.Globalization;

namespace ObbTextGenerator;

/// <summary>
/// Renders lines of text onto the canvas and produces rich metadata for downstream writers.
/// </summary>
public sealed class TextLineStage(
    ITextProvider textProvider,
    IFontProvider fontProvider,
    IColorProvider colorProvider,
    TextLineStageSettings settings) : RenderStageBase(settings)
{
    private readonly ITextProvider _textProvider = textProvider;
    private readonly IFontProvider _fontProvider = fontProvider;
    private readonly IColorProvider _colorProvider = colorProvider;
    private readonly TextLineStageSettings _settings = settings;

    public override string Name => "Text/Line";

    protected override void ApplyCore(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;
        if (random.NextDouble() > _settings.Probability) return;

        int count = _settings.Count.SampleInt(random);
        int generatedCount = 0;
        for (int i = 0; i < count; i++)
        {
            if (GenerateAndRenderLine(context, window))
            {
                generatedCount++;
            }
        }

        context.AddTrace($"text-line: class={_settings.ClassId} generated={generatedCount}");
    }

    private bool GenerateAndRenderLine(RenderContext context, RenderWindow window)
    {
        var random = context.Settings.Random;

        #region 1. Data Preparation
        
        var text = _textProvider.GetText(context);
        if (string.IsNullOrWhiteSpace(text)) return false;
        var layoutHints = ResolveLayoutHints(context, text);

        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = _colorProvider.GetColor(context);

        var typeface = _fontProvider.GetTypeface(context);
        var fontSize = _fontProvider.GetSize(context);
        using var font = new SKFont(typeface, fontSize);
        
        #endregion

        #region 2. Metrics & Surveying

        var measuredBlock = MeasureTextBlock(text, font, paint, layoutHints);
        
        #endregion

        #region 3. Placement & Collision Detection

        SKPoint origin = SKPoint.Empty;
        float rotation = 0;
        bool placed = false;
        var imageBounds = SKRect.Create(0, 0, context.Width, context.Height);
        var allowedBounds = SKRect.Intersect(window.Bounds, imageBounds);
        if (allowedBounds.IsEmpty)
        {
            return false;
        }

        var horizontalPadding = MathF.Min(20f, allowedBounds.Width * 0.1f);
        var verticalPadding = MathF.Min(20f, allowedBounds.Height * 0.1f);

        var minOriginX = allowedBounds.Left - measuredBlock.TightBounds.Left + horizontalPadding;
        var maxOriginX = allowedBounds.Right - measuredBlock.TightBounds.Right - horizontalPadding;
        var minOriginY = allowedBounds.Top - measuredBlock.TightBounds.Top + verticalPadding;
        var maxOriginY = allowedBounds.Bottom - measuredBlock.TightBounds.Bottom - verticalPadding;

        if (maxOriginX <= minOriginX || maxOriginY <= minOriginY) return false;
        var explicitCenter = _settings.X != null || _settings.Y != null;
        var centerBounds = UnionBounds(
            measuredBlock.TightBounds,
            measuredBlock.FontMetricsBounds,
            measuredBlock.CapHeightBounds,
            measuredBlock.XHeightBounds);

        // Try to place without collision
        for (int attempt = 0; attempt < 20; attempt++)
        {
            rotation = _settings.Rotation.Sample(random);

            if (explicitCenter)
            {
                var centerX = ResolveCenterCoordinate(_settings.X, allowedBounds.Left, allowedBounds.Width, allowedBounds.MidX, random);
                var centerY = ResolveCenterCoordinate(_settings.Y, allowedBounds.Top, allowedBounds.Height, allowedBounds.MidY, random);
                origin = CalculateOriginFromCenter(new SKPoint(centerX, centerY), rotation, centerBounds);
            }
            else
            {
                origin = new SKPoint(
                    NextFloat(context.Settings.Random, minOriginX, maxOriginX),
                    NextFloat(context.Settings.Random, minOriginY, maxOriginY)
                );
            }

            if (!AllBoxesFitWithinBounds(
                origin,
                rotation,
                measuredBlock.TightBounds,
                measuredBlock.FontMetricsBounds,
                measuredBlock.CapHeightBounds,
                measuredBlock.XHeightBounds,
                allowedBounds))
            {
                if (explicitCenter)
                {
                    break;
                }

                continue;
            }

            if (TryRegisterMeasuredBlockCollision(context, measuredBlock, origin, rotation, _settings.ClassId))
            {
                placed = true;
                break;
            }

            if (explicitCenter)
            {
                break;
            }
        }

        if (!placed) return false;

        #endregion

        #region 4. Rendering

        context.Canvas.Save();
        context.Canvas.RotateDegrees(rotation, origin.X, origin.Y);
        DrawMeasuredTextBlock(context, origin, measuredBlock, font, paint);
        context.Canvas.Restore();

        #endregion

        #region 5. Populate Rich Descriptor

        AddLineDescriptors(context, measuredBlock, text, origin, rotation, typeface, fontSize, paint);

        #endregion
        return true;
    }

    private TextLayoutHints ResolveLayoutHints(RenderContext context, string text)
    {
        if (_textProvider is not ITextLayoutHintsProvider layoutHintsProvider)
        {
            return TextLayoutHints.Default;
        }

        return layoutHintsProvider.GetLayoutHints(context, text);
    }

    private MeasuredTextBlock MeasureTextBlock(string text, SKFont font, SKPaint paint, TextLayoutHints layoutHints)
    {
        var lines = SplitLines(text);
        font.GetFontMetrics(out var metrics);

        var nativeLineHeight = Math.Max(0f, metrics.Descent - metrics.Ascent);
        var additionalLineSpacing = layoutHints.LineSpacing * font.Size;
        var baselineStep = nativeLineHeight + additionalLineSpacing;
        if (baselineStep <= 0f)
        {
            baselineStep = nativeLineHeight > 0f ? nativeLineHeight : font.Size;
        }

        var measuredLines = new List<(string Text, float Width, SKRect TightBounds, int GapCount)>(lines.Length);
        var maxWidth = 0f;

        foreach (var line in lines)
        {
            var lineWidth = font.MeasureText(line, out var tightBounds, paint);
            var gapCount = CountWordGaps(line);
            measuredLines.Add((line, lineWidth, tightBounds, gapCount));
            maxWidth = Math.Max(maxWidth, lineWidth);
        }

        var lineLayouts = new List<TextBlockLineLayout>(measuredLines.Count);
        var tightBoundsUnion = SKRect.Empty;
        var fontMetricsBoundsUnion = SKRect.Empty;
        var capHeightBoundsUnion = SKRect.Empty;
        var xHeightBoundsUnion = SKRect.Empty;

        for (var lineIndex = 0; lineIndex < measuredLines.Count; lineIndex++)
        {
            var line = measuredLines[lineIndex];
            var baselineY = baselineStep * lineIndex;
            var isLastLine = lineIndex == measuredLines.Count - 1;
            var useJustify = layoutHints.Alignment == RandomCharTextAlignment.Justify && !isLastLine && line.GapCount > 0;
            var widthForAlignment = useJustify ? maxWidth : line.Width;
            var offsetX = ResolveLineOffset(layoutHints.Alignment, maxWidth, line.Width, useJustify);
            var additionalSpaceWidth = useJustify
                ? Math.Max(0f, maxWidth - line.Width) / line.GapCount
                : 0f;

            var lineLayout = new TextBlockLineLayout
            {
                Text = line.Text,
                OffsetX = offsetX,
                BaselineY = baselineY,
                Width = line.Width,
                TightBounds = new SKRect(
                    useJustify ? 0f : line.TightBounds.Left,
                    line.TightBounds.Top,
                    useJustify ? maxWidth : line.TightBounds.Right,
                    line.TightBounds.Bottom),
                FontMetricsBounds = new SKRect(
                    0f,
                    metrics.Ascent,
                    widthForAlignment,
                    metrics.Descent),
                CapHeightBounds = new SKRect(
                    0f,
                    -metrics.CapHeight,
                    widthForAlignment,
                    0f),
                XHeightBounds = new SKRect(
                    0f,
                    -metrics.XHeight,
                    widthForAlignment,
                    0f),
                IsJustified = useJustify,
                AdditionalSpaceWidth = additionalSpaceWidth
            };
            lineLayouts.Add(lineLayout);

            var lineTightBounds = new SKRect(
                offsetX + lineLayout.TightBounds.Left,
                baselineY + lineLayout.TightBounds.Top,
                offsetX + lineLayout.TightBounds.Right,
                baselineY + lineLayout.TightBounds.Bottom);

            var lineFontMetricsBounds = OffsetRect(lineLayout.FontMetricsBounds, offsetX, baselineY);
            var lineCapHeightBounds = OffsetRect(lineLayout.CapHeightBounds, offsetX, baselineY);
            var lineXHeightBounds = OffsetRect(lineLayout.XHeightBounds, offsetX, baselineY);

            tightBoundsUnion = UnionOrTakeFirst(tightBoundsUnion, lineTightBounds, lineIndex);
            fontMetricsBoundsUnion = UnionOrTakeFirst(fontMetricsBoundsUnion, lineFontMetricsBounds, lineIndex);
            capHeightBoundsUnion = UnionOrTakeFirst(capHeightBoundsUnion, lineCapHeightBounds, lineIndex);
            xHeightBoundsUnion = UnionOrTakeFirst(xHeightBoundsUnion, lineXHeightBounds, lineIndex);
        }

        return new MeasuredTextBlock
        {
            Lines = lineLayouts,
            TightBounds = tightBoundsUnion,
            FontMetricsBounds = fontMetricsBoundsUnion,
            CapHeightBounds = capHeightBoundsUnion,
            XHeightBounds = xHeightBoundsUnion,
            TotalWidth = maxWidth
        };
    }

    private SKRect OffsetRect(SKRect rect, float offsetX, float offsetY)
    {
        return new SKRect(
            rect.Left + offsetX,
            rect.Top + offsetY,
            rect.Right + offsetX,
            rect.Bottom + offsetY);
    }

    private string[] SplitLines(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.None);
    }

    private int CountWordGaps(string line)
    {
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Math.Max(0, words.Length - 1);
    }

    private float ResolveLineOffset(RandomCharTextAlignment alignment, float maxWidth, float lineWidth, bool useJustify)
    {
        if (useJustify)
        {
            return 0f;
        }

        return alignment switch
        {
            RandomCharTextAlignment.Right => maxWidth - lineWidth,
            RandomCharTextAlignment.Center => (maxWidth - lineWidth) / 2f,
            _ => 0f
        };
    }

    private SKRect UnionOrTakeFirst(SKRect current, SKRect candidate, int index)
    {
        if (index == 0)
        {
            return candidate;
        }

        return SKRect.Union(current, candidate);
    }

    private void DrawMeasuredTextBlock(RenderContext context, SKPoint origin, MeasuredTextBlock measuredBlock, SKFont font, SKPaint paint)
    {
        foreach (var line in measuredBlock.Lines)
        {
            var lineX = origin.X + line.OffsetX;
            var lineY = origin.Y + line.BaselineY;

            if (line.IsJustified)
            {
                DrawJustifiedLine(context.Canvas, line.Text, lineX, lineY, line.AdditionalSpaceWidth, font, paint);
                continue;
            }

            context.Canvas.DrawText(line.Text, lineX, lineY, font, paint);
        }
    }

    private bool TryRegisterMeasuredBlockCollision(
        RenderContext context,
        MeasuredTextBlock measuredBlock,
        SKPoint blockOrigin,
        float rotation,
        int classId)
    {
        var lineCollisionPoints = new List<(string Text, SKPoint[] Points)>(measuredBlock.Lines.Count);
        foreach (var line in measuredBlock.Lines)
        {
            var lineOrigin = new SKPoint(blockOrigin.X + line.OffsetX, blockOrigin.Y + line.BaselineY);
            var collisionPoints = GetGlobalPoints(lineOrigin, rotation, line.TightBounds);
            if (context.HasCollision(collisionPoints))
            {
                return false;
            }

            lineCollisionPoints.Add((line.Text, collisionPoints));
        }

        var collisionLayer = context.GetOrCreateAnnotationLayer("collision");
        foreach (var line in lineCollisionPoints)
        {
            collisionLayer.Annotations.Add(new ShapeAnnotation
            {
                Points = line.Points,
                Text = line.Text,
                ClassId = classId
            });
        }

        return true;
    }

    private void AddLineDescriptors(
        RenderContext context,
        MeasuredTextBlock measuredBlock,
        string blockText,
        SKPoint blockOrigin,
        float rotation,
        SKTypeface typeface,
        float fontSize,
        SKPaint paint)
    {
        var blockId = Guid.NewGuid().ToString("N");
        var blockMetadata = BuildBlockMetadata(blockId, measuredBlock, blockOrigin, rotation);

        for (var lineIndex = 0; lineIndex < measuredBlock.Lines.Count; lineIndex++)
        {
            var line = measuredBlock.Lines[lineIndex];
            var lineOrigin = new SKPoint(blockOrigin.X + line.OffsetX, blockOrigin.Y + line.BaselineY);
            var descriptor = new TextLineDescriptor
            {
                Text = line.Text,
                ClassId = _settings.ClassId,
                Origin = lineOrigin,
                Rotation = rotation,
                BlockId = blockId,
                LineIndexInBlock = lineIndex,
                LineCountInBlock = measuredBlock.Lines.Count,
                BlockOrigin = blockOrigin,
                Font = new SKFont(typeface, fontSize),
                Paint = paint.Clone(),
                TightBounds = line.TightBounds,
                FontMetricsBounds = line.FontMetricsBounds,
                CapHeightBounds = line.CapHeightBounds,
                XHeightBounds = line.XHeightBounds,
                TotalWidth = line.Width,
                BlockTightBounds = measuredBlock.TightBounds,
                BlockFontMetricsBounds = measuredBlock.FontMetricsBounds,
                BlockCapHeightBounds = measuredBlock.CapHeightBounds,
                BlockXHeightBounds = measuredBlock.XHeightBounds
            };

            descriptor.Metadata["textBlockText"] = blockText;
            descriptor.Metadata["textBlockId"] = blockId;
            descriptor.Metadata["textBlockLineIndex"] = lineIndex.ToString(CultureInfo.InvariantCulture);
            descriptor.Metadata["textBlockLineCount"] = measuredBlock.Lines.Count.ToString(CultureInfo.InvariantCulture);

            foreach (var (key, value) in blockMetadata)
            {
                descriptor.Metadata[key] = value;
            }

            context.TextLines.Add(descriptor);
        }
    }

    private Dictionary<string, string> BuildBlockMetadata(string blockId, MeasuredTextBlock measuredBlock, SKPoint blockOrigin, float rotation)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["textBlockId"] = blockId
        };

        AppendBlockBoundsMetadata(metadata, "textBlockTight", blockOrigin, rotation, measuredBlock.TightBounds);
        AppendBlockBoundsMetadata(metadata, "textBlockFontMetrics", blockOrigin, rotation, measuredBlock.FontMetricsBounds);
        AppendBlockBoundsMetadata(metadata, "textBlockCapHeight", blockOrigin, rotation, measuredBlock.CapHeightBounds);
        AppendBlockBoundsMetadata(metadata, "textBlockXHeight", blockOrigin, rotation, measuredBlock.XHeightBounds);

        return metadata;
    }

    private void AppendBlockBoundsMetadata(
        Dictionary<string, string> metadata,
        string prefix,
        SKPoint origin,
        float rotation,
        SKRect localRect)
    {
        var points = GetGlobalPoints(origin, rotation, localRect);
        metadata[$"{prefix}Obb"] = SerializePoints(points);
        metadata[$"{prefix}Box"] = SerializeAxisAlignedBox(points);
    }

    private string SerializePoints(SKPoint[] points)
    {
        return string.Join(
            " ",
            points.Select(point => $"{point.X.ToStringInvariant()},{point.Y.ToStringInvariant()}"));
    }

    private string SerializeAxisAlignedBox(SKPoint[] points)
    {
        var minX = points.Min(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxX = points.Max(point => point.X);
        var maxY = points.Max(point => point.Y);
        return $"{minX.ToStringInvariant()},{minY.ToStringInvariant()},{maxX.ToStringInvariant()},{maxY.ToStringInvariant()}";
    }

    private void DrawJustifiedLine(SKCanvas canvas, string text, float x, float y, float additionalSpaceWidth, SKFont font, SKPaint paint)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            canvas.DrawText(text, x, y, font, paint);
            return;
        }

        var currentX = x;
        var baseSpaceWidth = font.MeasureText(" ", paint);

        for (var wordIndex = 0; wordIndex < words.Length; wordIndex++)
        {
            var word = words[wordIndex];
            canvas.DrawText(word, currentX, y, font, paint);
            currentX += font.MeasureText(word, paint);

            if (wordIndex < words.Length - 1)
            {
                currentX += baseSpaceWidth + additionalSpaceWidth;
            }
        }
    }

    private bool AllBoxesFitWithinBounds(
        SKPoint origin,
        float rotation,
        SKRect tightBounds,
        SKRect fontMetricsBounds,
        SKRect capHeightBounds,
        SKRect xHeightBounds,
        SKRect allowedBounds)
    {
        return BoxFitsWithinBounds(origin, rotation, tightBounds, allowedBounds)
            && BoxFitsWithinBounds(origin, rotation, fontMetricsBounds, allowedBounds)
            && BoxFitsWithinBounds(origin, rotation, capHeightBounds, allowedBounds)
            && BoxFitsWithinBounds(origin, rotation, xHeightBounds, allowedBounds);
    }

    private bool BoxFitsWithinBounds(SKPoint origin, float rotation, SKRect localBounds, SKRect allowedBounds)
    {
        var points = GetGlobalPoints(origin, rotation, localBounds);
        return AreAllPointsInsideBounds(points, allowedBounds);
    }

    private SKPoint[] GetGlobalPoints(SKPoint origin, float rotation, SKRect localBounds)
    {
        var matrix = SKMatrix.CreateRotationDegrees(rotation, origin.X, origin.Y);

        var p1 = matrix.MapPoint(origin.X + localBounds.Left, origin.Y + localBounds.Top);
        var p2 = matrix.MapPoint(origin.X + localBounds.Right, origin.Y + localBounds.Top);
        var p3 = matrix.MapPoint(origin.X + localBounds.Right, origin.Y + localBounds.Bottom);
        var p4 = matrix.MapPoint(origin.X + localBounds.Left, origin.Y + localBounds.Bottom);

        return [p1, p2, p3, p4];
    }

    private bool AreAllPointsInsideBounds(SKPoint[] points, SKRect allowedBounds)
    {
        const float epsilon = 0.001f;

        foreach (var point in points)
        {
            if (point.X < allowedBounds.Left - epsilon || point.X > allowedBounds.Right + epsilon)
            {
                return false;
            }

            if (point.Y < allowedBounds.Top - epsilon || point.Y > allowedBounds.Bottom + epsilon)
            {
                return false;
            }
        }

        return true;
    }

    private float NextFloat(Random random, float minValue, float maxValue)
    {
        if (maxValue <= minValue)
        {
            return minValue;
        }

        return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
    }

    private float ResolveCenterCoordinate(SampledValueSpec? spec, float start, float reference, float fallback, Random random)
    {
        if (spec == null)
        {
            return fallback;
        }

        return start + spec.Sample(random, reference);
    }

    private SKPoint CalculateOriginFromCenter(SKPoint center, float rotation, SKRect bounds)
    {
        var localCenter = new SKPoint(
            (bounds.Left + bounds.Right) / 2f,
            (bounds.Top + bounds.Bottom) / 2f);

        var angleRad = rotation * (MathF.PI / 180f);
        var cos = MathF.Cos(angleRad);
        var sin = MathF.Sin(angleRad);
        var rotatedCenter = new SKPoint(
            localCenter.X * cos - localCenter.Y * sin,
            localCenter.X * sin + localCenter.Y * cos);

        return new SKPoint(center.X - rotatedCenter.X, center.Y - rotatedCenter.Y);
    }

    private SKRect UnionBounds(params SKRect[] bounds)
    {
        if (bounds.Length == 0)
        {
            return SKRect.Empty;
        }

        var result = bounds[0];
        for (var index = 1; index < bounds.Length; index++)
        {
            result = SKRect.Union(result, bounds[index]);
        }

        return result;
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return $"text-line-stage class={_settings.ClassId}";
    }
}
