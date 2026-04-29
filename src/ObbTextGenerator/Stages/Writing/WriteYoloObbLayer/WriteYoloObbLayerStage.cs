using System.Globalization;
using System.Text;

namespace ObbTextGenerator;

public sealed class WriteYoloObbLayerStage(WriteYoloObbLayerStageSettings settings, FullConfig fullConfig) : IPipelineStage
{
    private readonly WriteYoloObbLayerStageSettings _settings = settings;
    private readonly FullConfig _fullConfig = fullConfig;

    public string Name => "Write/YoloObbLayer";

    public void Apply(RenderContext context)
    {
        var fullOutputDir = Path.Combine(_fullConfig.General.OutputRoot, context.SetName, _settings.Path);
        Directory.CreateDirectory(fullOutputDir);

        var fileName = $"{context.SampleIndex:D6}.txt";
        var filePath = Path.Combine(fullOutputDir, fileName);

        context.AnnotationLayers.TryGetValue(_settings.AnnotationLayer, out var sourceLayer);

        AnnotationLayer? feedbackLayer = null;
        if(!string.IsNullOrEmpty(_settings.FeedbackLayer))
        {
            feedbackLayer = context.GetOrCreateAnnotationLayer(_settings.FeedbackLayer);
        }

        var builder = new StringBuilder();
        var writtenCount = 0;
        var skippedCount = 0;

        if(sourceLayer != null)
        {
            foreach(var annotation in sourceLayer.Annotations)
            {
                if(annotation.Points.Length < 4)
                {
                    skippedCount++;
                    continue;
                }

                builder.Append(annotation.ClassId.ToString(CultureInfo.InvariantCulture));

                for(var pointIndex = 0; pointIndex < 4; pointIndex++)
                {
                    var point = annotation.Points[pointIndex];
                    var normalizedX = YoloCoordinateTools.NormalizeAndClampX(point.X, context.Width);
                    var normalizedY = YoloCoordinateTools.NormalizeAndClampY(point.Y, context.Height);
                    builder.AppendFormat(CultureInfo.InvariantCulture, " {0:F6} {1:F6}", normalizedX, normalizedY);
                }

                builder.AppendLine();
                writtenCount++;

                feedbackLayer?.Annotations.Add(new ShapeAnnotation
                {
                    Points = annotation.Points.ToArray(),
                    ClassId = annotation.ClassId,
                    Text = annotation.Text
                });
            }
        }

        File.WriteAllText(filePath, builder.ToString());
        context.AddTrace(
            $"write-yolo-obb-layer: {_settings.Path}",
            $"source-layer={_settings.AnnotationLayer} feedback-layer={_settings.FeedbackLayer} written={writtenCount} skipped={skippedCount}");
    }
}
