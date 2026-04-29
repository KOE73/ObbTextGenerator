namespace ObbTextGenerator;

public sealed class RectFixedLayerSettings : PatternLayerSettingsBase
{
    /// <summary>
    /// [x, y, width, height] relative to tile bounds (0.0 to 1.0)
    /// </summary>
    public float[] Rect { get; init; } = { 0, 0, 1.0f, 1.0f };
}
