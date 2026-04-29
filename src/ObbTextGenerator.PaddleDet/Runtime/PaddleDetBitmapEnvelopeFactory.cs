using SkiaSharp;

namespace ObbTextGenerator;

/// <summary>
/// Creates the immutable bitmap envelope from the current sample image.
/// This is the first step of the PaddleDet plugin pipeline.
/// </summary>
public static class PaddleDetBitmapEnvelopeFactory
{
    public static PaddleDetBitmapEnvelope Create(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        return new PaddleDetBitmapEnvelope(
            Bitmap: bitmap,
            Width: bitmap.Width,
            Height: bitmap.Height,
            ColorType: bitmap.ColorType,
            AlphaType: bitmap.AlphaType);
    }
}
