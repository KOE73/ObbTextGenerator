namespace ObbTextGenerator;

public sealed class TextLayoutHints
{
    public static TextLayoutHints Default { get; } = new();

    /// <summary>
    /// Additional spacing between baselines in font-size units.
    /// Value 0 keeps only the native font line height.
    /// </summary>
    public float LineSpacing { get; init; }

    public RandomCharTextAlignment Alignment { get; init; } = RandomCharTextAlignment.Left;
}
