namespace ObbTextGenerator;

public sealed class FileLinesProvider : ITextProvider
{
    private readonly string[] _lines;

    public FileLinesProvider(FileLinesProviderSettings settings, string configDir)
    {
        string fullPath = System.IO.Path.IsPathRooted(settings.Path) 
            ? settings.Path 
            : System.IO.Path.Combine(configDir, settings.Path);

        if (File.Exists(fullPath))
        {
            _lines = File.ReadAllLines(fullPath)
                         .Where(l => !string.IsNullOrWhiteSpace(l))
                         .ToArray();
        }
        else
        {
            _lines = [$"ERROR: File not found: {settings.Path}"];
        }
    }

    public string GetText(RenderContext context)
    {
        if (_lines.Length == 0) return string.Empty;
        return _lines[context.Settings.Random.Next(_lines.Length)];
    }
}
