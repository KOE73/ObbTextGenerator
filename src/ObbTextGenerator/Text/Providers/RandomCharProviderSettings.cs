namespace ObbTextGenerator;

public sealed class RandomCharProviderSettings : TextProviderSettingsBase
{
    /// <summary>
    /// Explicit characters to use, or a preset name like "latin", "digits", "latin-digits".
    /// </summary>
    public string CharSet { get; init; } = "latin-digits";
    
    public SampledValueSpec Words { get; init; } = SampledValueSpec.Parse("1..3");
    
    public SampledValueSpec WordLength { get; init; } = SampledValueSpec.Parse("3..8");

    /// <summary>
    /// Number of lines in the generated text block.
    /// </summary>
    public SampledValueSpec Lines { get; init; } = SampledValueSpec.Parse("1");

    /// <summary>
    /// Additional spacing between lines in font-size units.
    /// Value 0 keeps only the native line height of the font.
    /// </summary>
    public SampledValueSpec LineSpacing { get; init; } = SampledValueSpec.Parse("0");

    /// <summary>
    /// Allowed alignment modes for the generated text block.
    /// One mode is sampled randomly for each generated block.
    /// </summary>
    public List<RandomCharTextAlignment> Alignments { get; init; } =
    [
        RandomCharTextAlignment.Left,
        RandomCharTextAlignment.Right,
        RandomCharTextAlignment.Center,
        RandomCharTextAlignment.Justify
    ];

    /// <summary>
    /// If true, capitalizes the first character of the string and makes the rest lowercase.
    /// </summary>
    public bool SentenceCase { get; init; } = false;
}
