namespace ObbTextGenerator;

public interface ITextLayoutHintsProvider
{
    TextLayoutHints GetLayoutHints(RenderContext context, string text);
}
