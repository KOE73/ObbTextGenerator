namespace ObbTextGenerator;

public static class ColorProviderFactory
{
    public static IColorProvider Create(ColorProviderSettingsBase settings, StageFactoryContext context)
    {
        return settings switch
        {
            ConstantColorProviderSettings s => new ConstantColorProvider(s),
            RandomColorProviderSettings s => new RandomColorProvider(s),
            GrayColorProviderSettings s => new GrayColorProvider(s),
            FromSchemeColorProviderSettings s => new FromSchemeColorProvider(s, context),
            _ => throw new NotSupportedException($"Color provider type '{settings?.GetType().Name ?? "null"}' is not supported.")
        };
    }
}
