namespace ObbTextGenerator;

public sealed class PipelineStageRegistry
{
    private readonly Dictionary<Type, IPipelineStageFactory> _factories = new();

    public void Register(IPipelineStageFactory factory)
    {
        _factories.Add(factory.SettingsType, factory);
    }

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var settingsType = settings.GetType();
        if(!_factories.TryGetValue(settingsType, out var factory))
            throw new InvalidOperationException($"Pipeline stage factory is not registered for settings type '{settingsType.Name}'.");

        return factory.Create(settings, context);
    }

    public List<IPipelineStage> CreateAll(IEnumerable<StageSettingsBase> settingsItems, StageFactoryContext context)
    {
        var stages = new List<IPipelineStage>();

        foreach (var settings in settingsItems)
        {
            stages.Add(Create(settings, context));
        }

        return stages;
    }
}
