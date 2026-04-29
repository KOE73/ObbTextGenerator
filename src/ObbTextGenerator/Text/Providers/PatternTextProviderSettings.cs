namespace ObbTextGenerator;

public sealed class PatternTextProviderSettings : TextProviderSettingsBase
{
    /// <summary>
    /// List of patterns to randomly choose from.
    /// Example: "{L}{N}-{NN} 50KG"
    /// </summary>
    public List<string> Templates { get; init; } = new();
}
