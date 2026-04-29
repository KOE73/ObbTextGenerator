using SkiaSharp;

namespace ObbTextGenerator;

public enum TextBoundingBoxType
{
    Tight,
    FontMetrics,
    CapHeight,
    XHeight
}

/// <summary>
/// A comprehensive descriptor of a rendered text line, containing all metrics and states 
/// necessary for writers to interpret and generate various specific bounding box formats.
/// </summary>
public sealed class TextLineDescriptor
{
    #region Identity & Basic State
    
    /// <summary>
    /// The generated text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Target object detection class ID.
    /// </summary>
    public int ClassId { get; init; }

    /// <summary>
    /// The baseline origin point where the text was rendered.
    /// </summary>
    public SKPoint Origin { get; init; }

    /// <summary>
    /// Rotation in degrees relative to the origin.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Identifier of the multi-line text block this line belongs to.
    /// Empty for standalone lines.
    /// </summary>
    public string BlockId { get; init; } = string.Empty;

    /// <summary>
    /// Zero-based index of this line inside the block.
    /// </summary>
    public int LineIndexInBlock { get; init; }

    /// <summary>
    /// Total number of lines inside the block.
    /// </summary>
    public int LineCountInBlock { get; init; } = 1;

    /// <summary>
    /// Baseline origin of the whole text block.
    /// For single-line text this is equal to <see cref="Origin"/>.
    /// </summary>
    public SKPoint BlockOrigin { get; init; }

    #endregion

    #region Font & Style Information
    
    /// <summary>
    /// The font used for rendering.
    /// </summary>
    public required SKFont Font { get; init; }

    /// <summary>
    /// The paint used for rendering (contains color, stroke, etc.).
    /// </summary>
    public required SKPaint Paint { get; init; }

    #endregion

    #region Metric Presets (Local Space)

    /// <summary>
    /// Bounding rectangle strictly encompassing the rendered pixels (MeasureText implementation).
    /// </summary>
    public SKRect TightBounds { get; init; }

    /// <summary>
    /// Bounding rectangle based on global font metrics (Ascent to Descent). 
    /// Constant height for the same font regardless of specific characters.
    /// </summary>
    public SKRect FontMetricsBounds { get; init; }

    /// <summary>
    /// Bounding rectangle based on CapHeight (Baseline to CapHeight). 
    /// Good for strict uppercase-like alignments.
    /// </summary>
    public SKRect CapHeightBounds { get; init; }

    /// <summary>
    /// Bounding rectangle based on XHeight (Baseline to XHeight).
    /// </summary>
    public SKRect XHeightBounds { get; init; }

    /// <summary>
    /// The total advance width of the text.
    /// </summary>
    public float TotalWidth { get; init; }

    /// <summary>
    /// Bounding rectangle of the whole text block strictly encompassing rendered pixels.
    /// </summary>
    public SKRect BlockTightBounds { get; init; }

    /// <summary>
    /// Bounding rectangle of the whole text block based on font metrics.
    /// </summary>
    public SKRect BlockFontMetricsBounds { get; init; }

    /// <summary>
    /// Bounding rectangle of the whole text block based on cap height.
    /// </summary>
    public SKRect BlockCapHeightBounds { get; init; }

    /// <summary>
    /// Bounding rectangle of the whole text block based on x-height.
    /// </summary>
    public SKRect BlockXHeightBounds { get; init; }

    /// <summary>
    /// Optional metadata associated with this text line.
    /// </summary>
    public Dictionary<string, string> Metadata { get; } = new();

    #endregion

    #region Transformation Helpers

    /// <summary>
    /// Converts a local-space rectangle to a global-space polyline (4 points) 
    /// using the origin and rotation of this text line.
    /// </summary>
    public SKPoint[] GetGlobalPoints(SKRect localRect)
    {
        var p1 = MapPoint(localRect.Left, localRect.Top, Origin);
        var p2 = MapPoint(localRect.Right, localRect.Top, Origin);
        var p3 = MapPoint(localRect.Right, localRect.Bottom, Origin);
        var p4 = MapPoint(localRect.Left, localRect.Bottom, Origin);

        return [p1, p2, p3, p4];
    }

    public SKPoint[] GetBlockGlobalPoints(TextBoundingBoxType boxType)
    {
        var localRect = boxType switch
        {
            TextBoundingBoxType.Tight => BlockTightBounds,
            TextBoundingBoxType.CapHeight => BlockCapHeightBounds,
            TextBoundingBoxType.XHeight => BlockXHeightBounds,
            _ => BlockFontMetricsBounds
        };

        var p1 = MapPoint(localRect.Left, localRect.Top, BlockOrigin);
        var p2 = MapPoint(localRect.Right, localRect.Top, BlockOrigin);
        var p3 = MapPoint(localRect.Right, localRect.Bottom, BlockOrigin);
        var p4 = MapPoint(localRect.Left, localRect.Bottom, BlockOrigin);

        return [p1, p2, p3, p4];
    }

    private SKPoint MapPoint(float x, float y, SKPoint origin)
    {
        var matrix = SKMatrix.CreateRotationDegrees(Rotation, origin.X, origin.Y);
        return matrix.MapPoint(origin.X + x, origin.Y + y);
    }

    #endregion
}
