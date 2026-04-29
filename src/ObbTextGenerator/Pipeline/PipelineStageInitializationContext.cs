namespace ObbTextGenerator;

/// <summary>
/// Shared initialization data available before the generation loop starts.
/// </summary>
public sealed class PipelineStageInitializationContext
{
    /// <summary>
    /// Global generation settings used by the current pipeline run.
    /// </summary>
    public required GenerationSettings GenerationSettings { get; init; }

    /// <summary>
    /// Resolved number of parallel workers that will execute Apply() calls.
    /// </summary>
    public required int MaxParallelism { get; init; }
}
