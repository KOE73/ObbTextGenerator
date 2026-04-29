namespace ObbTextGenerator;

public sealed class PipelineProgramStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(PipelineProgramStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var programSettings = (PipelineProgramStageSettings)settings;
        var program = context.FullConfig.PipelinePrograms.FirstOrDefault(item => item.Name.Equals(programSettings.ProgramName, StringComparison.OrdinalIgnoreCase));
        if (program == null)
        {
            throw new InvalidOperationException($"Pipeline program '{programSettings.ProgramName}' was not found.");
        }

        var stages = context.StageRegistry.CreateAll(program.Stages, context);
        return new PipelineProgramStage(programSettings, program.Name, stages);
    }
}
