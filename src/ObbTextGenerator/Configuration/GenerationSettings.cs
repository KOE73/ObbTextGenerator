namespace ObbTextGenerator;

/// <summary>
/// Global generation settings, including canvas size and export rules.
/// </summary>
public sealed class GenerationSettings
{
    /// <summary>
    /// Target image width.
    /// </summary>
    public int Width { get; init; } = 1024;

    /// <summary>
    /// Target image height.
    /// </summary>
    public int Height { get; init; } = 1024;

    /// <summary>
    /// Root directory for saving all generated artifacts.
    /// </summary>
    public string OutputRoot { get; init; } = "outputs";

    /// <summary>
    /// Root directory for shared YAML resources, such as FontGroups.
    /// Relative paths are resolved from the configuration file directory.
    /// </summary>
    public string ResourceRoot { get; init; } = "Resources";

    /// <summary>
    /// Total number of images to generate.
    /// </summary>
    public int SampleCount { get; init; } = 100;

    /// <summary>
    /// Percentage of samples to go into the training set (0.0 to 1.0).
    /// </summary>
    public double TrainSplit { get; init; } = 0.8;

    /// <summary>
    /// Maximum number of samples generated in parallel.
    /// Use 0 or less for automatic selection based on CPU count.
    /// </summary>
    public int MaxParallelism { get; init; }
}
