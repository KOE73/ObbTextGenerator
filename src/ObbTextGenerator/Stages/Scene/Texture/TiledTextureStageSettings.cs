namespace ObbTextGenerator;

public sealed class TiledTextureStageSettings : RenderStageSettingsBase
{
    public SampledValueSpec TileSize { get; init; } = SampledValueSpec.Parse("10..40");

    public SampledValueSpec Alpha { get; init; } = SampledValueSpec.Parse("0.05..0.2");

    /// <summary>
    /// Random rotation for the texture.
    /// </summary>
    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0+-180");
}
