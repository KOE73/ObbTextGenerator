namespace ObbTextGenerator;

public sealed class WriteYoloBoxStageSettings : StageSettingsBase
{
    /// <summary>
    /// Path relative to the sample's set directory (train/val).
    /// </summary>
    public string Path { get; init; } = "labels";

    /// <summary>
    /// Which pre-calculated box type to use from the descriptor to derive AABB.
    /// </summary>
    public TextBoundingBoxType BoxType { get; init; } = TextBoundingBoxType.FontMetrics;

    /// <summary>
    /// Optional layer name to write the resulting AABB back for visualization.
    /// </summary>
    public string FeedbackLayer { get; init; } = "debug_box";
}
