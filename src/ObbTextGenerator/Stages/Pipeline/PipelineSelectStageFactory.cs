namespace ObbTextGenerator;

public sealed class PipelineSelectStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(PipelineSelectStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var selectSettings = (PipelineSelectStageSettings)settings;
        var programs = ResolvePrograms(selectSettings, context);
        return new PipelineSelectStage(selectSettings, programs);
    }

    private static List<CompiledPipelineProgram> ResolvePrograms(PipelineSelectStageSettings settings, StageFactoryContext context)
    {
        IEnumerable<PipelineProgramDefinition> definitions = context.FullConfig.PipelinePrograms;

        if (!string.IsNullOrWhiteSpace(settings.Group))
        {
            definitions = definitions.Where(item => item.Group.Equals(settings.Group, StringComparison.OrdinalIgnoreCase));
        }

        var result = new List<CompiledPipelineProgram>();
        foreach (var definition in definitions)
        {
            var stages = context.StageRegistry.CreateAll(definition.Stages, context);
            result.Add(new CompiledPipelineProgram
            {
                Name = definition.Name,
                Group = definition.Group,
                Weight = definition.Weight,
                Stages = stages
            });
        }

        return result;
    }
}
