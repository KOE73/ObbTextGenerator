namespace ObbTextGenerator;

/// <summary>
/// Settings for the PaddleDet stage.
/// The stage reads the rendered bitmap, produces a detector mask Mat, stores intermediate data in the sample context,
/// and writes the postprocessed OBB result into an annotation layer.
/// </summary>
public sealed class PaddleDetStageSettings : StageSettingsBase
{
    public string ModelPath { get; init; } = string.Empty;

    /// <summary>
    /// ONNX inference backend name from NeuroModFlowNet.ONNX.InferenceBackend.
    /// Supported values currently exposed by the package enum: Cpu, CUDA, TensorRT.
    /// YAML parsing is handled case-insensitively, so values like "cuda" are valid.
    /// </summary>
    public string Backend { get; init; } = "Cpu";

    /// <summary>
    /// Controls runner access under parallel generation.
    /// Supported values: SharedLocked, PerThread.
    /// </summary>
    public PaddleDetRunnerAccessMode RunnerAccessMode { get; init; } = PaddleDetRunnerAccessMode.SharedLocked;

    public string OutputLayer { get; init; } = "paddle_test_obb";

    public string MaskDataKey { get; init; } = "paddledet.mask";

    public string PolygonDataKey { get; init; } = "paddledet.polygons";

    public int ClassId { get; init; } = 0;

    public string LabelText { get; init; } = "paddledet";
}
