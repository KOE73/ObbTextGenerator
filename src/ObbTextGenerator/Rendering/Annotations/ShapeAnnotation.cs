using SkiaSharp;

namespace ObbTextGenerator;

/// <summary>
/// A generic annotation representing a geometric shape (polyline/polygon) in the image.
/// This replaces the specific ObbAnnotation to support any form of visual feedback.
/// </summary>
public sealed class ShapeAnnotation
{
    /// <summary>
    /// The coordinates of the shape's vertices.
    /// </summary>
    public required SKPoint[] Points { get; init; }

    /// <summary>
    /// The object detection class ID.
    /// </summary>
    public int ClassId { get; init; } = 0;

    /// <summary>
    /// Optional metadata associated with this specific annotation.
    /// </summary>
    public Dictionary<string, string> Metadata { get; } = new();

    /// <summary>
    /// The text content if this shape represents a text line.
    /// </summary>
    public string Text { get; init; } = string.Empty;
}

/// <summary>
/// A named collection of shape annotations, representing a specific type of result or debug info.
/// </summary>
public sealed class AnnotationLayer
{
    /// <summary>
    /// List of all shapes in this layer.
    /// </summary>
    public List<ShapeAnnotation> Annotations { get; } = new();
}
