namespace ObbTextGenerator;

public sealed class WriteContextDumpStage(
    WriteContextDumpStageSettings settings,
    string outputRoot) : IPipelineStage
{
    private readonly WriteContextDumpStageSettings _settings = settings;
    private readonly string _outputRoot = outputRoot;

    public string Name => $"Write/ContextDump ({_settings.Path})";

    public void Apply(RenderContext context)
    {
        var fullOutputDirectory = Path.Combine(_outputRoot, context.SetName, _settings.Path);
        Directory.CreateDirectory(fullOutputDirectory);

        var fileExtension = NormalizeExtension(_settings.FileExtension);
        var fileName = $"{context.SampleIndex:D6}.{fileExtension}";
        var filePath = Path.Combine(fullOutputDirectory, fileName);

        var formatter = new SpectreContextDumpFormatter(_settings);
        var text = formatter.Format(context);

        File.WriteAllText(filePath, text);
        context.AddTrace($"write-context-dump: {_settings.Path}", $"file={fileName}");
    }

    private static string NormalizeExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "txt";
        }

        return value.Trim().TrimStart('.');
    }
}
