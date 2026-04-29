namespace ObbTextGenerator;

public sealed class RenderTraceEntry
{
    public int Depth { get; init; }

    public required string Summary { get; init; }

    public string? Details { get; init; }

    public string GetText(RenderTraceVerbosity verbosity)
    {
        if (verbosity == RenderTraceVerbosity.Verbose && !string.IsNullOrWhiteSpace(Details))
        {
            return $"{Summary} {Details}";
        }

        return Summary;
    }

    public string GetIndentedText(RenderTraceVerbosity verbosity, string indentText)
    {
        var text = GetText(verbosity);
        if (Depth <= 0 || string.IsNullOrEmpty(indentText))
        {
            return text;
        }

        return string.Concat(Enumerable.Repeat(indentText, Depth)) + text;
    }
}
