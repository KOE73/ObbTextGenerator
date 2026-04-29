using NeuroModFlowNet.ONNX;
using OpenCvSharp;

namespace ObbTextGenerator;

/// <summary>
/// Holds one initialized PaddleDet model session and its paired runner.
/// </summary>
public sealed class PaddleDetSession
{
    public required OnnxModel Model { get; init; }

    public required IRunner<Mat, Mat> Runner { get; init; }
}
