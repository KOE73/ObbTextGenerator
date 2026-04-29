namespace ObbTextGenerator;

public sealed class PipelineBlockStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(PipelineBlockStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var blockSettings = (PipelineBlockStageSettings)settings;
        var stages = context.StageRegistry.CreateAll(blockSettings.Stages, context);
        return new PipelineBlockStage(blockSettings, stages);
    }
}
