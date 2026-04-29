namespace ObbTextGenerator;

public static class TextProviderFactory
{
    public static ITextProvider Create(TextProviderSettingsBase settings, StageFactoryContext context)
    {
        return settings switch
        {
            ConstantTextProviderSettings s => new ConstantTextProvider(s.Text),
            RandomCharProviderSettings s => new RandomCharProvider(s),
            PatternTextProviderSettings s => new PatternTextProvider(s),
            FileLinesProviderSettings s => new FileLinesProvider(s, context.ConfigDirectory),
            WeightedCompositeProviderSettings s => new WeightedCompositeProvider(s, inner => Create(inner, context)),
            _ => throw new NotSupportedException($"Text provider type '{settings.GetType().Name}' is not supported.")
        };
    }
}
