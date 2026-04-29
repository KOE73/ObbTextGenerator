using OpenCvSharp;

namespace ObbTextGenerator;

/// <summary>
/// Runtime strategy for executing PaddleDet inference after one-time stage initialization.
/// </summary>
public interface IPaddleDetExecutionRuntime
{
    Mat Predict(Mat inputMat);
}
