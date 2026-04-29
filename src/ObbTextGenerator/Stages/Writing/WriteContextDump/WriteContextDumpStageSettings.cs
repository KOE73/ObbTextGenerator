namespace ObbTextGenerator;

public sealed class WriteContextDumpStageSettings : StageSettingsBase
{
    public string Path { get; init; } = "context_dump";

    public string FileExtension { get; init; } = "txt";

    public bool IncludeSettingsTree { get; init; } = true;

    public bool IncludeTextLinesTree { get; init; } = true;

    public bool IncludeAnnotationLayersTree { get; init; } = true;

    public bool IncludeSampleDataTree { get; init; } = true;

    public bool IncludeTraceTree { get; init; } = true;

    public int MaxCollectionItems { get; init; } = 256;

    public int MaxDepth { get; init; } = 8;

    public ContextDumpTableBorderStyle TableBorderStyle { get; init; } = ContextDumpTableBorderStyle.Ascii;

    public ContextDumpTreeGuideStyle TreeGuideStyle { get; init; } = ContextDumpTreeGuideStyle.Ascii;
}
