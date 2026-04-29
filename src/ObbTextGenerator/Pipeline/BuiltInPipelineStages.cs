namespace ObbTextGenerator;

/// <summary>
/// Static registry of all built-in pipeline stages available for YAML configuration.
/// </summary>
public static class BuiltInPipelineStages
{
    public static void RegisterAll(PipelineStageRegistrationCollection registrations)
    {
        // Pipeline Composition
        registrations.Register("pipeline-block", new PipelineBlockStageFactory());
        registrations.Register("pipeline-program", new PipelineProgramStageFactory());
        registrations.Register("pipeline-repeat", new PipelineRepeatStageFactory());
        registrations.Register("pipeline-select", new PipelineSelectStageFactory());

        // Core & Logic
        registrations.Register("scheme-selector", new SchemeSelectorStageFactory());

        // Background & Texture
        registrations.Register("background-fill-color", new BackgroundFillColorStageFactory());
        registrations.Register("image-folder", new ImageFolderStageFactory());
        registrations.Register("noise-background", new NoiseBackgroundStageFactory());
        registrations.Register("tiled-texture", new TiledTextureStageFactory());
        registrations.Register("tiled-pattern", new TiledPatternStageFactory());

        // Text
        registrations.Register("text-line", new TextLineStageFactory());

        // Rendering & Effects
        registrations.Register("spotlight", new SpotlightStageFactory());
        registrations.Register("camera-effects", new CameraEffectsStageFactory());
        registrations.Register("annotation-overlay", new AnnotationOverlayStageFactory());
        registrations.Register("overlay-annotations", new AnnotationOverlayStageFactory());
        
        // Exporters / Writers
        registrations.Register("write-image", new WriteImageStageFactory());
        registrations.Register("write-context-dump", new WriteContextDumpStageFactory());
        registrations.Register("write-debug-preview", new WriteDebugPreviewStageFactory());
        registrations.Register("write-yolo-obb", new WriteYoloObbStageFactory());
        registrations.Register("write-yolo-obb-layer", new WriteYoloObbLayerStageFactory());
        registrations.Register("write-yolo-box", new WriteYoloBoxStageFactory());

        // Misc
    }
}
