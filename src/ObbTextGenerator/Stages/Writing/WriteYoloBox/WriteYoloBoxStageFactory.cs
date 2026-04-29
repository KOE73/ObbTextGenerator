namespace ObbTextGenerator;

public sealed class WriteYoloBoxStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(WriteYoloBoxStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new WriteYoloBoxStage((WriteYoloBoxStageSettings)settings, context.FullConfig);
    }
}
