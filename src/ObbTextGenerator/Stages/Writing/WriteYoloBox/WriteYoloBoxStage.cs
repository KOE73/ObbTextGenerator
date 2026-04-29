using System.Globalization;
using System.Text;

namespace ObbTextGenerator;

public sealed class WriteYoloBoxStage(WriteYoloBoxStageSettings settings, FullConfig fullConfig) : IPipelineStage
{
    private readonly WriteYoloBoxStageSettings _settings = settings;
    private readonly FullConfig _fullConfig = fullConfig;

    public string Name => "Write/YoloBox";

    public void Apply(RenderContext context)
    {
        var fullOutputDir = Path.Combine(_fullConfig.General.OutputRoot, context.SetName, _settings.Path);
        Directory.CreateDirectory(fullOutputDir);

        var fileName = $"{context.SampleIndex:D6}.txt";
        var filePath = Path.Combine(fullOutputDir, fileName);

        AnnotationLayer? feedbackLayer = null;
        if(!string.IsNullOrEmpty(_settings.FeedbackLayer))
        {
            feedbackLayer = context.GetOrCreateAnnotationLayer(_settings.FeedbackLayer);
        }

        var builder = new StringBuilder();
        foreach(var descriptor in context.TextLines)
        {
            var localBox = _settings.BoxType switch
            {
                TextBoundingBoxType.Tight => descriptor.TightBounds,
                TextBoundingBoxType.CapHeight => descriptor.CapHeightBounds,
                TextBoundingBoxType.XHeight => descriptor.XHeightBounds,
                _ => descriptor.FontMetricsBounds
            };

            var points = descriptor.GetGlobalPoints(localBox);

            var minimumX = float.MaxValue;
            var maximumX = float.MinValue;
            var minimumY = float.MaxValue;
            var maximumY = float.MinValue;

            foreach(var point in points)
            {
                minimumX = Math.Min(minimumX, point.X);
                maximumX = Math.Max(maximumX, point.X);
                minimumY = Math.Min(minimumY, point.Y);
                maximumY = Math.Max(maximumY, point.Y);
            }

            var clippedMinimumX = YoloCoordinateTools.ClampX(minimumX, context.Width);
            var clippedMaximumX = YoloCoordinateTools.ClampX(maximumX, context.Width);
            var clippedMinimumY = YoloCoordinateTools.ClampY(minimumY, context.Height);
            var clippedMaximumY = YoloCoordinateTools.ClampY(maximumY, context.Height);

            var width = clippedMaximumX - clippedMinimumX;
            var height = clippedMaximumY - clippedMinimumY;
            var centerX = clippedMinimumX + width / 2.0f;
            var centerY = clippedMinimumY + height / 2.0f;

            var normalizedCenterX = YoloCoordinateTools.NormalizeAndClampX(centerX, context.Width);
            var normalizedCenterY = YoloCoordinateTools.NormalizeAndClampY(centerY, context.Height);
            var normalizedWidth = YoloCoordinateTools.NormalizeAndClampWidth(width, context.Width);
            var normalizedHeight = YoloCoordinateTools.NormalizeAndClampHeight(height, context.Height);

            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0} {1:F6} {2:F6} {3:F6} {4:F6}",
                descriptor.ClassId,
                normalizedCenterX,
                normalizedCenterY,
                normalizedWidth,
                normalizedHeight);
            builder.AppendLine();

            if(feedbackLayer != null)
            {
                feedbackLayer.Annotations.Add(new ShapeAnnotation
                {
                    Points =
                    [
                        new(minimumX, minimumY),
                        new(maximumX, minimumY),
                        new(maximumX, maximumY),
                        new(minimumX, maximumY)
                    ],
                    ClassId = descriptor.ClassId,
                    Text = descriptor.Text
                });
            }
        }

        File.WriteAllText(filePath, builder.ToString());
        context.AddTrace($"write-yolo-box: {_settings.Path}", $"source=text-lines layer={_settings.FeedbackLayer}");
    }
}
