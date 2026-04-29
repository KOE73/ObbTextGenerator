namespace ObbTextGenerator;

public sealed class ConstantTextProvider(string text) : ITextProvider
{
    private readonly string _text = text;

    public string GetText(RenderContext context) => _text;
}
