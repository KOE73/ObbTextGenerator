namespace ObbTextGenerator;

public sealed class WeightedCompositeProviderSettings : TextProviderSettingsBase
{
    public List<WeightedItem> Items { get; init; } = new();
}
