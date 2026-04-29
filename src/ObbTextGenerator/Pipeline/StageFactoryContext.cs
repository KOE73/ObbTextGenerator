namespace ObbTextGenerator
{
    public sealed class StageFactoryContext
    {
        /// <summary>
        /// Global generation settings shared by all stages.
        /// </summary>
        public required RenderSettings RenderSettings { get; init; }

        /// <summary>
        /// Full configuration object.
        /// </summary>
        public required FullConfig FullConfig { get; init; }

        /// <summary>
        /// Stage registry used to recursively build nested pipelines.
        /// </summary>
        public required PipelineStageRegistry StageRegistry { get; init; }

        /// <summary>
        /// Directory of the loaded YAML config file.
        /// Useful for resolving relative paths in stage configs.
        /// </summary>
        public required string ConfigDirectory { get; init; }

        /// <summary>
        /// Root directory for output files.
        /// </summary>
        public required string OutputRoot { get; init; }

        /// <summary>
        /// Root directory for shared YAML resources.
        /// </summary>
        public required string ResourceRoot { get; init; }

        /// <summary>
        /// Shared service provider for optional stage dependencies.
        /// </summary>
        public IServiceProvider? Services { get; init; }
    }
}
