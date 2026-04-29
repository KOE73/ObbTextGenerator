using OpenCvSharp;

namespace ObbTextGenerator;

/// <summary>
/// Executes inference through a dedicated session per worker thread.
/// </summary>
public sealed class PerThreadPaddleDetExecutionRuntime : IPaddleDetExecutionRuntime
{
    private readonly IReadOnlyList<PaddleDetSession> _sessions;
    private readonly ThreadLocal<int> _threadSessionIndex;
    private int _nextSessionIndex;

    public PerThreadPaddleDetExecutionRuntime(IReadOnlyList<PaddleDetSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        if (sessions.Count == 0)
        {
            throw new InvalidOperationException("Per-thread PaddleDet runtime requires at least one session.");
        }

        _sessions = sessions;
        _threadSessionIndex = new ThreadLocal<int>(RentSessionIndex);
    }

    public Mat Predict(Mat inputMat)
    {
        var sessionIndex = _threadSessionIndex.Value;
        var session = _sessions[sessionIndex];
        return session.Runner.Predict(inputMat);
    }

    private int RentSessionIndex()
    {
        var rawIndex = Interlocked.Increment(ref _nextSessionIndex) - 1;
        return rawIndex % _sessions.Count;
    }
}
