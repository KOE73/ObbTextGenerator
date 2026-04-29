namespace ObbTextGenerator;

public sealed class WeightedItem
{
    public required TextProviderSettingsBase Provider { get; init; }
    public double Weight { get; init; } = 1.0;
}

public sealed class WeightedCompositeProvider : ITextProvider
{
    private record ItemEntry(ITextProvider Provider, double CumulativeWeight);
    private readonly List<ItemEntry> _items = new();
    private readonly double _totalWeight;

    public WeightedCompositeProvider(WeightedCompositeProviderSettings settings, Func<TextProviderSettingsBase, ITextProvider> factory)
    {
        double current = 0;
        foreach (var item in settings.Items)
        {
            current += item.Weight;
            _items.Add(new ItemEntry(factory(item.Provider), current));
        }
        _totalWeight = current;
    }

    public string GetText(RenderContext context)
    {
        if (_items.Count == 0) return string.Empty;

        double roll = context.Settings.Random.NextDouble() * _totalWeight;
        foreach (var entry in _items)
        {
            if (roll <= entry.CumulativeWeight)
                return entry.Provider.GetText(context);
        }

        return _items.Last().Provider.GetText(context);
    }
}
