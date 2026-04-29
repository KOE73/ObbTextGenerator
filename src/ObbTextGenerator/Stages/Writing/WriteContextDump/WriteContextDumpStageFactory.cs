namespace ObbTextGenerator;

public sealed class WriteContextDumpStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(WriteContextDumpStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new WriteContextDumpStage((WriteContextDumpStageSettings)settings, context.OutputRoot);
    }
}
