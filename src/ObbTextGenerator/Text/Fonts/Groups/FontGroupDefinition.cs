namespace ObbTextGenerator;

public sealed class FontGroupDefinition
{
    public string Name { get; init; } = string.Empty;

    public List<string> Families { get; init; } = [];
}
