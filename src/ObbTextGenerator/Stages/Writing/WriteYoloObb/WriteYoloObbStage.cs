using System.Globalization;
using System.Text;

namespace ObbTextGenerator;

public sealed class WriteYoloObbStage(WriteYoloObbStageSettings settings, FullConfig fullConfig) : IPipelineStage
{
    private readonly WriteYoloObbStageSettings _settings = settings;
    private readonly FullConfig _fullConfig = fullConfig;

    public string Name => "Write/YoloObb";

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

            var globalPoints = descriptor.GetGlobalPoints(localBox);

            builder.Append(descriptor.ClassId.ToString(CultureInfo.InvariantCulture));
            foreach(var point in globalPoints)
            {
                var normalizedX = YoloCoordinateTools.NormalizeAndClampX(point.X, context.Width);
                var normalizedY = YoloCoordinateTools.NormalizeAndClampY(point.Y, context.Height);
                builder.AppendFormat(CultureInfo.InvariantCulture, " {0:F6} {1:F6}", normalizedX, normalizedY);
            }

            builder.AppendLine();

            feedbackLayer?.Annotations.Add(new ShapeAnnotation
            {
                Points = globalPoints,
                ClassId = descriptor.ClassId,
                Text = descriptor.Text
            });
        }

        File.WriteAllText(filePath, builder.ToString());
        context.AddTrace($"write-yolo-obb: {_settings.Path}", $"source=text-lines layer={_settings.FeedbackLayer}");
    }
}
