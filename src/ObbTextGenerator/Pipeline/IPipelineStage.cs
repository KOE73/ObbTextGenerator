namespace ObbTextGenerator;
/// <summary>
/// A single pipeline stage that can read and modify the current rendering state.
///
/// The stage works against <see cref="RenderContext"/> and can do any of the following:
/// - draw new content onto the current canvas;
/// - inspect previously rendered content;
/// - apply geometric transforms to the image and all stored annotations;
/// - apply photometric effects to the image only;
/// - create or update one or more annotation layers.
///
/// The key design idea is that all stages use the same shared context.
/// This keeps the pipeline extensible and allows stages to be reordered freely.
/// </summary>
public interface IPipelineStage
{
    /// <summary>
    /// Gets a human-readable stage name.
    ///
    /// This is used for diagnostics, logs, progress reporting, debug overlays,
    /// and error messages. It should be stable and descriptive.
    /// Example values:
    /// "Background/ImageFolder"
    /// "Text/LineRenderer"
    /// "Geometry/Rotate"
    /// "Effects/JpegArtifacts"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies this stage to the current <see cref="RenderContext"/>.
    ///
    /// The stage is allowed to:
    /// - modify <see cref="RenderContext.Surface"/>;
    /// - add, remove, or update annotations in <see cref="RenderContext.AnnotationLayers"/>;
    /// - store intermediate objects in <see cref="RenderContext.Items"/>;
    /// - append debug information to <see cref="RenderContext.Messages"/>.
    ///
    /// The stage must honor <see cref="RenderContext.CancellationToken"/> when it performs
    /// non-trivial work or uses loops over many objects.
    /// </summary>
    /// <param name="context">Shared mutable state for the current render job.</param>
    void Apply(RenderContext context);
}

