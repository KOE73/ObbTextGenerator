namespace ObbTextGenerator;

public sealed class WriteYoloObbLayerStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(WriteYoloObbLayerStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new WriteYoloObbLayerStage((WriteYoloObbLayerStageSettings)settings, context.FullConfig);
    }
}
