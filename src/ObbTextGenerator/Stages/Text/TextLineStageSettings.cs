namespace ObbTextGenerator;

public sealed class TextLineStageSettings : RenderStageSettingsBase
{
    /// <summary>
    /// Configures how the text string is generated for each sample.
    /// </summary>
    public TextProviderSettingsBase Provider { get; init; } = new ConstantTextProviderSettings();

    /// <summary>
    /// Configures the font and size for the text.
    /// </summary>
    public FontProviderSettingsBase Font { get; init; } = new ConstantFontProviderSettings();

    /// <summary>
    /// Configures the color of the text.
    /// </summary>
    public ColorProviderSettingsBase Color { get; init; } = new ConstantColorProviderSettings();

    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public double Probability { get; init; } = 1.0;

    /// <summary>
    /// Optional X position of the rendered text center within the current render window.
    /// </summary>
    public SampledValueSpec? X { get; init; }

    /// <summary>
    /// Optional Y position of the rendered text center within the current render window.
    /// </summary>
    public SampledValueSpec? Y { get; init; }
    
    /// <summary>
    /// Rotation for text placement.
    /// </summary>
    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0+-15");

    /// <summary>
    /// Name of the layer to write annotations to.
    /// </summary>
    public string LayerName { get; init; } = "default";
    
    /// <summary>
    /// ClassId for object detection labels.
    /// </summary>
    public int ClassId { get; init; } = 0;
}
