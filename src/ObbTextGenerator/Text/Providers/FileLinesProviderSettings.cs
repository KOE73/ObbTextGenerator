namespace ObbTextGenerator;

public sealed class FileLinesProviderSettings : TextProviderSettingsBase
{
    /// <summary>
    /// Path to a text file containing one sample per line.
    /// </summary>
    public string Path { get; init; } = "words.txt";
}
