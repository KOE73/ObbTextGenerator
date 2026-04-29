using System.Text;
using System.Text.RegularExpressions;

namespace ObbTextGenerator;

public sealed class PatternTextProvider(PatternTextProviderSettings settings) : ITextProvider
{
    private readonly PatternTextProviderSettings _settings = settings;
    private static readonly Regex _patternRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    public string GetText(RenderContext context)
    {
        if (_settings.Templates.Count == 0) return string.Empty;

        var rnd = context.Settings.Random;
        string template = _settings.Templates[rnd.Next(_settings.Templates.Count)];

        return _patternRegex.Replace(template, m => 
        {
            string key = m.Groups[1].Value;
            
            // Handle {S:a|b|c}
            if (key.StartsWith("S:"))
            {
                var options = key.Substring(2).Split('|');
                return options[rnd.Next(options.Length)];
            }

            // Handle {N}, {NN}, {NNN}...
            if (key.All(c => c == 'N'))
            {
                var sb = new StringBuilder();
                for(int i=0; i<key.Length; i++) sb.Append(rnd.Next(10));
                return sb.ToString();
            }

            // Handle {L}, {LL}... (Latin Upper)
            if (key.All(c => c == 'L'))
            {
                var sb = new StringBuilder();
                for (int i = 0; i < key.Length; i++) sb.Append((char)rnd.Next('A', 'Z' + 1));
                return sb.ToString();
            }

            // Handle {C}, {CC}... (Cyrillic Upper)
            if (key.All(c => c == 'C'))
            {
                var sb = new StringBuilder();
                for (int i = 0; i < key.Length; i++) sb.Append((char)rnd.Next('А', 'Я' + 1));
                return sb.ToString();
            }

            return m.Value; // Keep as is if unrecognized
        });
    }
}
