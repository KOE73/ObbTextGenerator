using OpenCvSharp;

namespace ObbTextGenerator;

/// <summary>
/// Executes inference through one shared runner guarded by a lock.
/// </summary>
public sealed class SharedLockedPaddleDetExecutionRuntime(PaddleDetSession session) : IPaddleDetExecutionRuntime
{
    private readonly PaddleDetSession _session = session;
    private readonly Lock _predictLock = new();

    public Mat Predict(Mat inputMat)
    {
        lock (_predictLock)
        {
            return _session.Runner.Predict(inputMat);
        }
    }
}
