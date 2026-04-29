using System.Text;

namespace ObbTextGenerator;

public sealed class RandomCharProvider(RandomCharProviderSettings settings) : ITextProvider, ITextLayoutHintsProvider
{
    private readonly RandomCharProviderSettings _settings = settings;
    private readonly string _alphabet = ResolveAlphabet(settings.CharSet);

    public string GetText(RenderContext context)
    {
        var rnd = context.Settings.Random;
        var sb = new StringBuilder();
        var numberOfLines = Math.Max(1, _settings.Lines.SampleInt(rnd));

        for (var lineIndex = 0; lineIndex < numberOfLines; lineIndex++)
        {
            if (lineIndex > 0)
            {
                sb.Append('\n');
            }

            sb.Append(GenerateLine(rnd));
        }

        return sb.ToString();
    }

    public TextLayoutHints GetLayoutHints(RenderContext context, string text)
    {
        var random = context.Settings.Random;
        var alignments = _settings.Alignments;
        if (alignments.Count == 0)
        {
            alignments =
            [
                RandomCharTextAlignment.Left,
                RandomCharTextAlignment.Right,
                RandomCharTextAlignment.Center,
                RandomCharTextAlignment.Justify
            ];
        }

        var alignment = alignments[random.Next(alignments.Count)];
        var lineSpacing = _settings.LineSpacing.Sample(random);

        return new TextLayoutHints
        {
            Alignment = alignment,
            LineSpacing = lineSpacing
        };
    }

    private string GenerateLine(Random random)
    {
        var numberOfWords = Math.Max(1, _settings.Words.SampleInt(random));
        var builder = new StringBuilder();

        for (var wordIndex = 0; wordIndex < numberOfWords; wordIndex++)
        {
            if (wordIndex > 0)
            {
                builder.Append(' ');
            }

            var wordLength = Math.Max(1, _settings.WordLength.SampleInt(random));
            for (var characterIndex = 0; characterIndex < wordLength; characterIndex++)
            {
                builder.Append(_alphabet[random.Next(_alphabet.Length)]);
            }
        }

        var line = builder.ToString();
        if (!_settings.SentenceCase || line.Length == 0)
        {
            return line;
        }

        return char.ToUpper(line[0]) + line.Substring(1).ToLower();
    }

    private static string ResolveAlphabet(string charSet)
    {
        return charSet.ToLower() switch
        {
            "digits" => "0123456789",
            "latin" => "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",
            "latin-upper" => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "latin-digits" => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
            "cyrillic" => "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя",
            "cyrillic-upper" => "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ",
            _ => charSet // Use literal characters if not a preset
        };
    }
}
