using SkiaSharp;

namespace ObbTextGenerator;

/// <summary>
/// Immutable description of the current generated bitmap before it is converted into OpenCV Mats.
/// This keeps the bridge between generator runtime and PaddleDet plugin explicit and easy to debug.
/// </summary>
public sealed record PaddleDetBitmapEnvelope(
    SKBitmap Bitmap,
    int Width,
    int Height,
    SKColorType ColorType,
    SKAlphaType AlphaType);
