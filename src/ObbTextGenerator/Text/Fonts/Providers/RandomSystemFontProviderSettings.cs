using SkiaSharp;

namespace ObbTextGenerator;

public sealed class RandomSystemFontProviderSettings : FontProviderSettingsBase
{
    /// <summary>
    /// If specified, only fonts supporting this character will be chosen.
    /// Example: 'Я' for Cyrillic support.
    /// </summary>
    public char? RequiredGlyph { get; init; } = null;

    /// <summary>
    /// If specified, only families from these predefined font groups will be used.
    /// Groups are loaded from Resources/FontGroups/*.yaml.
    /// </summary>
    public List<string> IncludeGroups { get; init; } = [];

    /// <summary>
    /// Excludes families from these predefined font groups.
    /// Groups are loaded from Resources/FontGroups/*.yaml.
    /// </summary>
    public List<string> ExcludeGroups { get; init; } = [];

    /// <summary>
    /// Allowed font weights. Empty list means any weight.
    /// </summary>
    public List<SKFontStyleWeight> AllowedWeights { get; init; } = [SKFontStyleWeight.Normal];

    /// <summary>
    /// Allowed font widths. Empty list means any width.
    /// </summary>
    public List<SKFontStyleWidth> AllowedWidths { get; init; } = [SKFontStyleWidth.Normal];

    /// <summary>
    /// Allowed font slants. Empty list means any slant.
    /// </summary>
    public List<SKFontStyleSlant> AllowedSlants { get; init; } = [SKFontStyleSlant.Upright];
    
    public SampledValueSpec Size { get; init; } = SampledValueSpec.Parse("18..48");
}
