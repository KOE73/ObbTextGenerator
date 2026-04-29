namespace ObbTextGenerator;

public sealed class WriteYoloObbStageSettings : StageSettingsBase
{
    /// <summary>
    /// Path relative to the sample's set directory (train/val).
    /// </summary>
    public string Path { get; init; } = "labels_obb";

    /// <summary>
    /// Which pre-calculated box type to use from the descriptor.
    /// </summary>
    public TextBoundingBoxType BoxType { get; init; } = TextBoundingBoxType.FontMetrics;

    /// <summary>
    /// Optional layer name to write the resulting polyline back for visualization.
    /// </summary>
    public string FeedbackLayer { get; init; } = "debug_label";
}
