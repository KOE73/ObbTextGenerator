namespace ObbTextGenerator;

public sealed class WriteDebugPreviewStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(WriteDebugPreviewStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new WriteDebugPreviewStage((WriteDebugPreviewStageSettings)settings, context.OutputRoot);
    }
}
