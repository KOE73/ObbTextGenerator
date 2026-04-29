using SkiaSharp;

namespace ObbTextGenerator;

public static class SystemFontVariantResolver
{
    public static List<ResolvedSystemFontVariant> ResolveAllFamilies(string resourceRoot)
    {
        var settings = new RandomSystemFontProviderSettings
        {
            AllowedWeights = [],
            AllowedSlants = [],
            AllowedWidths = []
        };

        return Resolve(settings, resourceRoot);
    }

    public static List<ResolvedSystemFontVariant> Resolve(RandomSystemFontProviderSettings settings, string resourceRoot)
    {
        var fontManager = SKFontManager.Default;
        var familyNames = fontManager.GetFontFamilies();
        var selectedFamilies = GetSelectedFamilies(familyNames, settings, resourceRoot);
        var resolvedVariants = new List<ResolvedSystemFontVariant>();

        foreach (var family in selectedFamilies)
        {
            using var styleSet = fontManager.GetFontStyles(family);
            if (styleSet == null || styleSet.Count == 0)
            {
                TryAddFallbackVariant(fontManager, family, settings, resolvedVariants);
                continue;
            }

            for (int styleIndex = 0; styleIndex < styleSet.Count; styleIndex++)
            {
                var style = styleSet[styleIndex];
                if (!IsStyleAllowed(style, settings))
                {
                    continue;
                }

                var typeface = styleSet.CreateTypeface(styleIndex);
                if (typeface == null)
                {
                    continue;
                }

                if (settings.RequiredGlyph.HasValue && !typeface.ContainsGlyph(settings.RequiredGlyph.Value))
                {
                    typeface.Dispose();
                    continue;
                }

                var styleName = styleSet.GetStyleName(styleIndex);
                if (string.IsNullOrWhiteSpace(styleName))
                {
                    styleName = BuildStyleName(style);
                }

                resolvedVariants.Add(new ResolvedSystemFontVariant(family, styleName, style, typeface));
            }
        }

        return resolvedVariants;
    }

    private static void TryAddFallbackVariant(
        SKFontManager fontManager,
        string family,
        RandomSystemFontProviderSettings settings,
        List<ResolvedSystemFontVariant> resolvedVariants)
    {
        var fallbackTypeface = fontManager.MatchFamily(family, SKFontStyle.Normal);
        if (fallbackTypeface == null)
        {
            return;
        }

        var fallbackStyle = fallbackTypeface.FontStyle;
        if (!IsStyleAllowed(fallbackStyle, settings))
        {
            fallbackTypeface.Dispose();
            return;
        }

        if (settings.RequiredGlyph.HasValue && !fallbackTypeface.ContainsGlyph(settings.RequiredGlyph.Value))
        {
            fallbackTypeface.Dispose();
            return;
        }

        resolvedVariants.Add(new ResolvedSystemFontVariant(family, BuildStyleName(fallbackStyle), fallbackStyle, fallbackTypeface));
    }

    private static bool IsStyleAllowed(SKFontStyle style, RandomSystemFontProviderSettings settings)
    {
        var styleWeight = (SKFontStyleWeight)style.Weight;
        if (settings.AllowedWeights.Count > 0 && !settings.AllowedWeights.Contains(styleWeight))
        {
            return false;
        }

        var styleWidth = (SKFontStyleWidth)style.Width;
        if (settings.AllowedWidths.Count > 0 && !settings.AllowedWidths.Contains(styleWidth))
        {
            return false;
        }

        if (settings.AllowedSlants.Count > 0 && !settings.AllowedSlants.Contains(style.Slant))
        {
            return false;
        }

        return true;
    }

    private static List<string> GetSelectedFamilies(
        IEnumerable<string> familyNames,
        RandomSystemFontProviderSettings settings,
        string resourceRoot)
    {
        var availableFamilies = familyNames
            .Where(family => !string.IsNullOrWhiteSpace(family))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var includeFamilies = FontGroupLoader.LoadFamilies(settings.IncludeGroups, resourceRoot);
        var excludeFamilies = FontGroupLoader.LoadFamilies(settings.ExcludeGroups, resourceRoot);

        IEnumerable<string> selectedFamilies = availableFamilies;

        if (includeFamilies.Count > 0)
        {
            selectedFamilies = selectedFamilies.Where(includeFamilies.Contains);
        }

        if (excludeFamilies.Count > 0)
        {
            selectedFamilies = selectedFamilies.Where(family => !excludeFamilies.Contains(family));
        }

        var result = selectedFamilies.ToList();
        if (result.Count == 0)
        {
            throw new InvalidOperationException("Font selection produced an empty set. Check includeGroups/excludeGroups and installed system fonts.");
        }

        return result;
    }

    private static string BuildStyleName(SKFontStyle style)
    {
        var parts = new List<string>
        {
            style.Weight.ToString(),
            style.Width.ToString()
        };

        if (style.Slant != SKFontStyleSlant.Upright)
        {
            parts.Add(style.Slant.ToString());
        }

        return string.Join(" ", parts);
    }
}
