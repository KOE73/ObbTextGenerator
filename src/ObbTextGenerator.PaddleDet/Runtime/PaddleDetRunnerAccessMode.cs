namespace ObbTextGenerator;

/// <summary>
/// Controls how PaddleDet runner instances are used under parallel generation.
/// </summary>
public enum PaddleDetRunnerAccessMode
{
    /// <summary>
    /// One shared runner is created and guarded by a lock during Predict().
    /// </summary>
    SharedLocked,

    /// <summary>
    /// One separate model+runner pair is created for each worker thread.
    /// </summary>
    PerThread
}
