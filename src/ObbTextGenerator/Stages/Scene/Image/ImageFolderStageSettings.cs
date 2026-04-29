using SkiaSharp;

namespace ObbTextGenerator;

public sealed class ImageFolderStageSettings : RenderStageSettingsBase
{
    public required string Path { get; init; }

    public bool Recursive { get; init; }

    public SampledValueSpec Alpha { get; init; } = SampledValueSpec.Parse("1");

    public SKBlendMode BlendMode { get; init; } = SKBlendMode.SrcOver;

    public List<string> SearchPatterns { get; init; } = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp"];

    public ImageFolderAugmentationSettings Augment { get; init; } = new();
}
