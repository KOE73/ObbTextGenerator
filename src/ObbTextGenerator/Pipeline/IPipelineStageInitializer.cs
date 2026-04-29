namespace ObbTextGenerator;

/// <summary>
/// Optional stage contract for one-time pipeline initialization before sample generation starts.
/// </summary>
public interface IPipelineStageInitializer
{
    /// <summary>
    /// Initializes stage resources that are shared across sample execution.
    /// This method is called once before the parallel generation loop starts.
    /// </summary>
    void Initialize(PipelineStageInitializationContext context);
}
