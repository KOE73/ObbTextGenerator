namespace ObbTextGenerator;

public sealed class WriteYoloObbStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(WriteYoloObbStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new WriteYoloObbStage((WriteYoloObbStageSettings)settings, context.FullConfig);
    }
}
