using SkiaSharp;

namespace ObbTextGenerator;

/// <summary>
/// Shared state for a single render sample.
/// </summary>
public sealed class RenderContext
{
    private readonly Stack<RenderWindow> _renderWindows = new();
    private int _traceDepth;

    #region Core State

    public required RenderSettings Settings { get; init; }

    public required RenderSession Session { get; init; }

    public required SKBitmap Bitmap { get; init; }

    public required SKCanvas Canvas { get; init; }

    /// <summary>
    /// Index of the current sample in the batch.
    /// </summary>
    public int SampleIndex { get; init; }

    /// <summary>
    /// Name of the target set (e.g. "train", "val").
    /// </summary>
    public string SetName { get; init; } = "train";

    public RenderContext? ParentContext { get; init; }

    public SKMatrix LocalToParentTransform { get; init; } = SKMatrix.CreateIdentity();

    public float LocalToParentRotation { get; init; }

    #endregion

    #region Rich Data & Annotations

    /// <summary>
    /// High-level descriptors of all rendered text lines in this sample.
    /// This is the "Source of Truth" for Writers/Exporters.
    /// </summary>
    public List<TextLineDescriptor> TextLines { get; } = new();

    /// <summary>
    /// Multiple parallel layers of geometric shapes/annotations.
    /// Used for collisions, export feedback, and debug visualization.
    /// </summary>
    public ColorSchemeSettings? ActiveScheme { get; set; }

    public TiledPatternStageSettings? ActivePattern { get; set; }

    public string ActivePatternName { get; set; } = string.Empty;

    public Dictionary<string, AnnotationLayer> AnnotationLayers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Generic per-sample runtime data bag for intermediate stage results.
    /// Stages can exchange Mats, masks, polygons, and other transient artifacts through this collection.
    /// </summary>
    public Dictionary<string, object> SampleData { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<RenderTraceEntry> TraceEntries { get; } = new();

    #endregion

    #region Helpers

    /// <summary>
    /// Checks if the given points intersect with any existing annotation in the "collision" layer.
    /// </summary>
    public bool HasCollision(SKPoint[] points, string collisionLayer = "collision")
    {
        return Session.HasCollision(this, points, collisionLayer);
    }

    public bool HasLocalCollision(SKPoint[] points, string collisionLayer = "collision")
    {
        if (!AnnotationLayers.TryGetValue(collisionLayer, out var layer)) return false;

        foreach (var ann in layer.Annotations)
        {
            if (GeometryTools.PolygonsIntersect(points, ann.Points))
                return true;
        }
        return false;
    }

    public bool TryRegisterCollision(SKPoint[] points, string text, int classId, string collisionLayer = "collision")
    {
        return Session.TryRegisterCollision(this, points, text, classId, collisionLayer);
    }

    public AnnotationLayer GetOrCreateAnnotationLayer(string layerName)
    {
        if (!AnnotationLayers.TryGetValue(layerName, out var layer))
        {
            layer = new AnnotationLayer();
            AnnotationLayers[layerName] = layer;
        }

        return layer;
    }

    public void SetSampleData(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        SampleData[key] = value;
    }

    public bool TryGetSampleData<TValue>(string key, out TValue? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (SampleData.TryGetValue(key, out var storedValue) && storedValue is TValue typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public int Width => Settings.Width;

    public int Height => Settings.Height;

    public RenderWindow GetCurrentRenderWindow()
    {
        if (_renderWindows.Count > 0)
        {
            return _renderWindows.Peek();
        }

        return new RenderWindow(SKRect.Create(0, 0, Width, Height));
    }

    public void PushRenderWindow(RenderWindow window)
    {
        _renderWindows.Push(window);
    }

    public void PopRenderWindow()
    {
        if (_renderWindows.Count == 0)
        {
            return;
        }

        _renderWindows.Pop();
    }

    public void AddTrace(string summary, string? details = null)
    {
        TraceEntries.Add(new RenderTraceEntry
        {
            Depth = _traceDepth,
            Summary = summary,
            Details = details
        });
    }

    public RenderTraceScope BeginTraceScope(string summary, string? details = null)
    {
        AddTrace(summary, details);
        _traceDepth++;
        return new RenderTraceScope(this);
    }

    public void DecreaseTraceDepth()
    {
        if (_traceDepth <= 0)
        {
            return;
        }

        _traceDepth--;
    }

    #endregion
}
