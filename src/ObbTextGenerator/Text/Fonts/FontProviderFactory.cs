namespace ObbTextGenerator;

public static class FontProviderFactory
{
    public static IFontProvider Create(FontProviderSettingsBase settings, StageFactoryContext context)
    {
        return settings switch
        {
            ConstantFontProviderSettings s => new ConstantFontProvider(s),
            RandomSystemFontProviderSettings s => new RandomSystemFontProvider(s, context.ResourceRoot),
            _ => throw new NotSupportedException($"Font provider type '{settings.GetType().Name}' is not supported.")
        };
    }
}
