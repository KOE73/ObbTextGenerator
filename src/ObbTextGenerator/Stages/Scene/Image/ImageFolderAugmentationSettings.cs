namespace ObbTextGenerator;

public sealed class ImageFolderAugmentationSettings
{
    public ImageFitMode FitMode { get; init; } = ImageFitMode.Cover;

    public SampledValueSpec Scale { get; init; } = SampledValueSpec.Parse("1");

    public SampledValueSpec Rotation { get; init; } = SampledValueSpec.Parse("0");

    public double FlipXProbability { get; init; }

    public double FlipYProbability { get; init; }

    public SampledValueSpec Brightness { get; init; } = SampledValueSpec.Parse("0");

    public SampledValueSpec Contrast { get; init; } = SampledValueSpec.Parse("0");

    public SampledValueSpec Saturation { get; init; } = SampledValueSpec.Parse("0");

    public SampledValueSpec Blur { get; init; } = SampledValueSpec.Parse("0");
}
