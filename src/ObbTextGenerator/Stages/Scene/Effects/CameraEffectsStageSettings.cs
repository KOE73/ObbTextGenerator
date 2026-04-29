namespace ObbTextGenerator;

public sealed class CameraEffectsStageSettings : RenderStageSettingsBase
{
    /// <summary>
    /// Gauss blur sigma range. Simulate out-of-focus.
    /// </summary>
    public SampledValueSpec Blur { get; init; } = SampledValueSpec.Parse("0..1.2");

    /// <summary>
    /// Sensor grain opacity.
    /// </summary>
    public float GrainAlpha { get; init; } = 0.1f;
}
