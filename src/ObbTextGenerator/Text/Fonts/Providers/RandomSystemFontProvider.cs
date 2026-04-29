using SkiaSharp;

namespace ObbTextGenerator;

public sealed class RandomSystemFontProvider : IFontProvider
{
    private readonly List<ResolvedSystemFontVariant> _variants;
    private readonly SampledValueSpec _size;

    public RandomSystemFontProvider(RandomSystemFontProviderSettings settings, string resourceRoot)
    {
        _size = settings.Size;
        
        _variants = SystemFontVariantResolver.Resolve(settings, resourceRoot);

        if (_variants.Count == 0)
        {
            if (settings.RequiredGlyph.HasValue
                || settings.IncludeGroups.Count > 0
                || settings.ExcludeGroups.Count > 0
                || settings.AllowedWeights.Count > 0
                || settings.AllowedWidths.Count > 0
                || settings.AllowedSlants.Count > 0)
            {
                throw new InvalidOperationException("No system font variants matched the configured filters. Check groups, allowed style lists, requiredGlyph, and installed fonts.");
            }

            _variants.Add(new ResolvedSystemFontVariant("Default", "Default", SKFontStyle.Normal, SKTypeface.Default));
        }
    }

    public SKTypeface GetTypeface(RenderContext context)
    {
        return _variants[context.Settings.Random.Next(_variants.Count)].Typeface;
    }

    public float GetSize(RenderContext context)
    {
        return _size.Sample(context.Settings.Random);
    }
}
