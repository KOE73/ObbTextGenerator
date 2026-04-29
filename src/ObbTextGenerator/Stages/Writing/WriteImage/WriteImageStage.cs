using SkiaSharp;

namespace ObbTextGenerator;

public sealed class WriteImageStage(string subPath, string format, int quality, string outputRoot) : IPipelineStage
{
    private readonly string _subPath = subPath;
    private readonly string _formatStr = format;
    private readonly int _quality = quality;
    private readonly string _outputRoot = outputRoot;

    public string Name => $"Write/Image ({_subPath})";

    public void Apply(RenderContext context)
    {
        var fullOutputDir = System.IO.Path.Combine(_outputRoot, context.SetName, _subPath);
        Directory.CreateDirectory(fullOutputDir);

        var encodedFormat = ImageEncodingResolver.ResolveFormat(_formatStr);
        var ext = ImageEncodingResolver.ResolveExtension(encodedFormat);
        var fileName = $"{context.SampleIndex:D6}.{ext}";
        var filePath = System.IO.Path.Combine(fullOutputDir, fileName);

        using var image = SKImage.FromBitmap(context.Bitmap);
        using var data = image.Encode(encodedFormat, _quality);
        if (data == null) return;

        using var stream = File.OpenWrite(filePath);
        data.SaveTo(stream);

        context.AddTrace($"write-image: {_subPath}", $"file={fileName}");
    }
}
