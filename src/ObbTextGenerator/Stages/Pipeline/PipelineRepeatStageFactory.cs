namespace ObbTextGenerator;

public sealed class PipelineRepeatStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(PipelineRepeatStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var repeatSettings = (PipelineRepeatStageSettings)settings;
        var stages = context.StageRegistry.CreateAll(repeatSettings.Stages, context);
        return new PipelineRepeatStage(repeatSettings, stages);
    }
}
