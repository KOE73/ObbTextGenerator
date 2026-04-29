using OpenCvSharp;
using System;

namespace ObbTextGenerator;

/// <summary>
/// Converts the current generated image into an OpenCV BGR <see cref="Mat"/>.
/// This is the main handoff format for future ONNX-based PaddleDet inference.
/// </summary>
public static class PaddleDetInputMatFactory
{
    public static Mat CreateBgrMat(PaddleDetBitmapEnvelope bitmapEnvelope)
    {
        ArgumentNullException.ThrowIfNull(bitmapEnvelope);

        var bitmap = bitmapEnvelope.Bitmap;
        var sourcePointer = bitmap.GetPixels();
        if (sourcePointer == IntPtr.Zero)
        {
            throw new InvalidOperationException("SKBitmap pixel buffer is not available.");
        }

        // We create a shallow BGRA Mat view over the SKBitmap buffer, clone it,
        // and convert the clone into the BGR format expected by the detector.
        using var bufferView = Mat.FromPixelData(
            bitmap.Height,
            bitmap.Width,
            MatType.CV_8UC4,
            sourcePointer,
            bitmap.RowBytes);

        using var bgraMat = bufferView.Clone();
        var bgrMat = new Mat();
        Cv2.CvtColor(bgraMat, bgrMat, ColorConversionCodes.BGRA2BGR);
        return bgrMat;
    }
}
