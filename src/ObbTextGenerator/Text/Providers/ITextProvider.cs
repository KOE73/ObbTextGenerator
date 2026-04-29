namespace ObbTextGenerator;

/// <summary>
/// Interface for objects that provide text strings for rendering.
/// </summary>
public interface ITextProvider
{
    /// <summary>
    /// Returns a text string for the current rendering context.
    /// </summary>
    string GetText(RenderContext context);
}
