using SkiaSharp;

namespace ObbTextGenerator;

public sealed class RenderWindow
{
    public RenderWindow(SKRect bounds, int index = 0)
    {
        Bounds = bounds;
        Index = index;
    }

    public SKRect Bounds { get; }

    public int Index { get; }

    public float Left => Bounds.Left;

    public float Top => Bounds.Top;

    public float Right => Bounds.Right;

    public float Bottom => Bounds.Bottom;

    public float Width => Bounds.Width;

    public float Height => Bounds.Height;

    public SKPoint Center => new(Bounds.MidX, Bounds.MidY);

    public SKPath CreateClipPath()
    {
        var path = new SKPath();
        path.AddRect(Bounds);
        return path;
    }
}
