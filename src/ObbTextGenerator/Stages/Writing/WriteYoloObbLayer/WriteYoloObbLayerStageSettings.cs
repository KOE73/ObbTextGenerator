namespace ObbTextGenerator;

public sealed class WriteYoloObbLayerStageSettings : StageSettingsBase
{
    /// <summary>
    /// Path relative to the sample's set directory.
    /// </summary>
    public string Path { get; init; } = "labels_obb_layer";

    /// <summary>
    /// Runtime annotation layer to export.
    /// </summary>
    public string AnnotationLayer { get; init; } = "default";

    /// <summary>
    /// Optional layer name to copy exported annotations back for visualization.
    /// </summary>
    public string FeedbackLayer { get; init; } = "debug_obb_layer";
}
