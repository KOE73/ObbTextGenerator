using SkiaSharp;

namespace ObbTextGenerator;

public static class ImageEncodingResolver
{
    public static SKEncodedImageFormat ResolveFormat(string formatText)
    {
        var normalized = formatText.Trim().ToLowerInvariant();
        return normalized switch
        {
            "jpg" => SKEncodedImageFormat.Jpeg,
            "jpeg" => SKEncodedImageFormat.Jpeg,
            _ when Enum.TryParse<SKEncodedImageFormat>(formatText, true, out var encodedFormat) => encodedFormat,
            _ => SKEncodedImageFormat.Png
        };
    }

    public static string ResolveExtension(SKEncodedImageFormat format)
    {
        return format switch
        {
            SKEncodedImageFormat.Jpeg => "jpg",
            _ => format.ToString().ToLowerInvariant()
        };
    }
}
