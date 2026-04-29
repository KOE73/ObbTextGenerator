namespace ObbTextGenerator;

public class RenderWindowSettings
{
    public RenderWindowMode Mode { get; init; } = RenderWindowMode.FullFrame;

    public RenderWindowPositionMode PositionMode { get; init; } = RenderWindowPositionMode.Center;

    public SampledValueSpec X { get; init; } = SampledValueSpec.Parse("0");

    public SampledValueSpec Y { get; init; } = SampledValueSpec.Parse("0");

    public SampledValueSpec Width { get; init; } = SampledValueSpec.Parse("100%");

    public SampledValueSpec Height { get; init; } = SampledValueSpec.Parse("100%");

    public SampledValueSpec Count { get; init; } = SampledValueSpec.Parse("1");

    public bool AllowOutOfBounds { get; init; } = true;

    public float MinVisibleAreaPercent { get; init; } = 50f;

    public bool AllowOverlap { get; init; } = true;
}
