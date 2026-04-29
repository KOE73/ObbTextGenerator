namespace ObbTextGenerator;

public static class YoloCoordinateTools
{
    public static float ClampX(float x, int imageWidth)
    {
        return Math.Clamp(x, 0.0f, imageWidth);
    }

    public static float ClampY(float y, int imageHeight)
    {
        return Math.Clamp(y, 0.0f, imageHeight);
    }

    public static float NormalizeAndClampX(float x, int imageWidth)
    {
        var clampedX = ClampX(x, imageWidth);
        var normalizedX = clampedX / imageWidth;
        return ClampNormalizedCoordinate(normalizedX);
    }

    public static float NormalizeAndClampY(float y, int imageHeight)
    {
        var clampedY = ClampY(y, imageHeight);
        var normalizedY = clampedY / imageHeight;
        return ClampNormalizedCoordinate(normalizedY);
    }

    public static float NormalizeAndClampWidth(float width, int imageWidth)
    {
        var normalizedWidth = width / imageWidth;
        return ClampNormalizedCoordinate(normalizedWidth);
    }

    public static float NormalizeAndClampHeight(float height, int imageHeight)
    {
        var normalizedHeight = height / imageHeight;
        return ClampNormalizedCoordinate(normalizedHeight);
    }

    private static float ClampNormalizedCoordinate(float value)
    {
        return Math.Clamp(value, 0.0f, 1.0f);
    }
}
