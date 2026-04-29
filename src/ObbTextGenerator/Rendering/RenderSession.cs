using SkiaSharp;
using System.Globalization;

namespace ObbTextGenerator;

public sealed class RenderSession
{
    public RenderContext CreateRootContext(RenderSettings settings, SKBitmap bitmap, SKCanvas canvas, int sampleIndex, string setName)
    {
        return new RenderContext
        {
            Session = this,
            Settings = settings,
            Bitmap = bitmap,
            Canvas = canvas,
            SampleIndex = sampleIndex,
            SetName = setName,
            ParentContext = null,
            LocalToParentRotation = 0f,
            LocalToParentTransform = SKMatrix.CreateIdentity()
        };
    }

    public bool TryRegisterCollision(RenderContext context, SKPoint[] localPoints, string text, int classId, string collisionLayer = "collision")
    {
        if (HasCollision(context, localPoints, collisionLayer))
        {
            return false;
        }

        var layer = context.GetOrCreateAnnotationLayer(collisionLayer);
        layer.Annotations.Add(new ShapeAnnotation
        {
            Points = localPoints,
            Text = text,
            ClassId = classId
        });

        return true;
    }

    public bool HasCollision(RenderContext context, SKPoint[] localPoints, string collisionLayer = "collision")
    {
        if (context.HasLocalCollision(localPoints, collisionLayer))
        {
            return true;
        }

        if (context.ParentContext == null)
        {
            return false;
        }

        var parentPoints = TransformPoints(localPoints, context.LocalToParentTransform);
        return HasCollision(context.ParentContext, parentPoints, collisionLayer);
    }

    public void ExecuteSurfaceProgram(
        RenderContext parentContext,
        string programName,
        IReadOnlyList<IPipelineStage> stages,
        PipelineProgramSurfaceSettings surfaceSettings,
        PipelineProgramPlaceSettings placeSettings)
    {
        var maxAttempts = Math.Max(1, surfaceSettings.MaxAttempts);
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var placements = RenderWindowResolver.Resolve(placeSettings, parentContext);
            if (placements.Count == 0)
            {
                continue;
            }

            foreach (var placementWindow in placements)
            {
                var rotationDeg = placeSettings.Rotation.Sample(parentContext.Settings.Random);
                var childContext = CreateSurfaceContext(parentContext, placementWindow, rotationDeg);

                try
                {
                    foreach (var stage in stages)
                    {
                        stage.Apply(childContext);
                    }

                    if (childContext.TextLines.Count < surfaceSettings.MinCreatedTextObjects)
                    {
                        parentContext.AddTrace(
                            $"surface discard: {programName}",
                            $"texts={childContext.TextLines.Count}/{surfaceSettings.MinCreatedTextObjects}");
                        continue;
                    }

                    CommitSurface(parentContext, childContext, placementWindow, rotationDeg, placeSettings);
                    parentContext.AddTrace(
                        $"surface commit: {programName}",
                        $"texts={childContext.TextLines.Count} rotation={rotationDeg:0.##}");
                    return;
                }
                finally
                {
                    childContext.Canvas.Dispose();
                    childContext.Bitmap.Dispose();
                }
            }
        }

        parentContext.AddTrace($"surface skip: {programName}", "no valid placement");
    }

    private RenderContext CreateSurfaceContext(RenderContext parentContext, RenderWindow placementWindow, float rotationDeg)
    {
        var width = Math.Max(1, (int)MathF.Round(placementWindow.Bounds.Width));
        var height = Math.Max(1, (int)MathF.Round(placementWindow.Bounds.Height));
        var bitmap = new SKBitmap(width, height);
        var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        return new RenderContext
        {
            Session = this,
            Settings = new RenderSettings(width, height, parentContext.Settings.Random),
            Bitmap = bitmap,
            Canvas = canvas,
            SampleIndex = parentContext.SampleIndex,
            SetName = parentContext.SetName,
            ParentContext = parentContext,
            ActiveScheme = parentContext.ActiveScheme,
            ActivePattern = parentContext.ActivePattern,
            ActivePatternName = parentContext.ActivePatternName,
            LocalToParentRotation = rotationDeg,
            LocalToParentTransform = CreateLocalToParentTransform(placementWindow.Bounds, rotationDeg)
        };
    }

    private void CommitSurface(
        RenderContext parentContext,
        RenderContext childContext,
        RenderWindow placementWindow,
        float rotationDeg,
        PipelineProgramPlaceSettings placeSettings)
    {
        parentContext.Canvas.Save();

        var bounds = placementWindow.Bounds;
        var centerX = bounds.MidX;
        var centerY = bounds.MidY;
        parentContext.Canvas.Translate(bounds.Left, bounds.Top);
        if (MathF.Abs(rotationDeg) > float.Epsilon)
        {
            parentContext.Canvas.RotateDegrees(rotationDeg, bounds.Width / 2f, bounds.Height / 2f);
        }

        using var paint = new SKPaint
        {
            BlendMode = placeSettings.BlendMode,
            Color = new SKColor(255, 255, 255, (byte)Math.Clamp(MathF.Round(placeSettings.Opacity * 255f), 0, 255))
        };

        using var image = SKImage.FromBitmap(childContext.Bitmap);
        parentContext.Canvas.DrawImage(image, 0, 0, paint);
        parentContext.Canvas.Restore();

        CommitAnnotations(parentContext, childContext);
        CommitTextLines(parentContext, childContext);
        CommitTrace(parentContext, childContext);
    }

    private void CommitAnnotations(RenderContext parentContext, RenderContext childContext)
    {
        foreach (var (layerName, childLayer) in childContext.AnnotationLayers)
        {
            var parentLayer = parentContext.GetOrCreateAnnotationLayer(layerName);
            foreach (var annotation in childLayer.Annotations)
            {
                parentLayer.Annotations.Add(new ShapeAnnotation
                {
                    Points = TransformPoints(annotation.Points, childContext.LocalToParentTransform),
                    ClassId = annotation.ClassId,
                    Text = annotation.Text
                });
                var committedAnnotation = parentLayer.Annotations[^1];
                foreach (var (key, value) in annotation.Metadata)
                {
                    committedAnnotation.Metadata[key] = value;
                }
            }
        }
    }

    private void CommitTextLines(RenderContext parentContext, RenderContext childContext)
    {
        foreach (var descriptor in childContext.TextLines)
        {
            parentContext.TextLines.Add(new TextLineDescriptor
            {
                Text = descriptor.Text,
                ClassId = descriptor.ClassId,
                Origin = childContext.LocalToParentTransform.MapPoint(descriptor.Origin),
                Rotation = descriptor.Rotation + childContext.LocalToParentRotation,
                BlockId = descriptor.BlockId,
                LineIndexInBlock = descriptor.LineIndexInBlock,
                LineCountInBlock = descriptor.LineCountInBlock,
                BlockOrigin = childContext.LocalToParentTransform.MapPoint(descriptor.BlockOrigin),
                Font = descriptor.Font,
                Paint = descriptor.Paint,
                TightBounds = descriptor.TightBounds,
                FontMetricsBounds = descriptor.FontMetricsBounds,
                CapHeightBounds = descriptor.CapHeightBounds,
                XHeightBounds = descriptor.XHeightBounds,
                TotalWidth = descriptor.TotalWidth,
                BlockTightBounds = descriptor.BlockTightBounds,
                BlockFontMetricsBounds = descriptor.BlockFontMetricsBounds,
                BlockCapHeightBounds = descriptor.BlockCapHeightBounds,
                BlockXHeightBounds = descriptor.BlockXHeightBounds
            });
            var committedDescriptor = parentContext.TextLines[^1];
            foreach (var (key, value) in descriptor.Metadata)
            {
                committedDescriptor.Metadata[key] = value;
            }

            RefreshBlockMetadata(committedDescriptor);
        }
    }

    private void RefreshBlockMetadata(TextLineDescriptor descriptor)
    {
        descriptor.Metadata["textBlockId"] = descriptor.BlockId;
        descriptor.Metadata["textBlockLineIndex"] = descriptor.LineIndexInBlock.ToString(CultureInfo.InvariantCulture);
        descriptor.Metadata["textBlockLineCount"] = descriptor.LineCountInBlock.ToString(CultureInfo.InvariantCulture);

        AppendBlockBoundsMetadata(descriptor, "textBlockTight", TextBoundingBoxType.Tight);
        AppendBlockBoundsMetadata(descriptor, "textBlockFontMetrics", TextBoundingBoxType.FontMetrics);
        AppendBlockBoundsMetadata(descriptor, "textBlockCapHeight", TextBoundingBoxType.CapHeight);
        AppendBlockBoundsMetadata(descriptor, "textBlockXHeight", TextBoundingBoxType.XHeight);
    }

    private void AppendBlockBoundsMetadata(TextLineDescriptor descriptor, string prefix, TextBoundingBoxType boxType)
    {
        var points = descriptor.GetBlockGlobalPoints(boxType);
        descriptor.Metadata[$"{prefix}Obb"] = SerializePoints(points);
        descriptor.Metadata[$"{prefix}Box"] = SerializeAxisAlignedBox(points);
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

    private void CommitTrace(RenderContext parentContext, RenderContext childContext)
    {
        foreach (var entry in childContext.TraceEntries)
        {
            parentContext.TraceEntries.Add(new RenderTraceEntry
            {
                Depth = entry.Depth + 1,
                Summary = entry.Summary,
                Details = entry.Details
            });
        }
    }

    private static SKPoint[] TransformPoints(SKPoint[] points, SKMatrix matrix)
    {
        var result = new SKPoint[points.Length];
        for (var index = 0; index < points.Length; index++)
        {
            result[index] = matrix.MapPoint(points[index]);
        }

        return result;
    }

    private static SKMatrix CreateLocalToParentTransform(SKRect bounds, float rotationDeg)
    {
        var translation = SKMatrix.CreateTranslation(bounds.Left, bounds.Top);
        if (MathF.Abs(rotationDeg) <= float.Epsilon)
        {
            return translation;
        }

        var rotation = SKMatrix.CreateRotationDegrees(rotationDeg, bounds.MidX, bounds.MidY);
        return SKMatrix.Concat(rotation, translation);
    }
}
