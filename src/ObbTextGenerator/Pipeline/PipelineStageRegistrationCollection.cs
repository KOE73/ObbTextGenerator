namespace ObbTextGenerator;

public sealed class PipelineStageRegistrationCollection
{
    private readonly Dictionary<string, Type> _stageSettingsTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Type> _registeredFactorySettingsTypes = [];
    private readonly PipelineStageRegistry _registry = new();

    public IReadOnlyDictionary<string, Type> StageSettingsTypes => _stageSettingsTypes;

    public PipelineStageRegistry Registry => _registry;

    public void Register(string stageType, IPipelineStageFactory factory)
    {
        if (string.IsNullOrWhiteSpace(stageType))
        {
            throw new ArgumentException("Stage type must not be empty.", nameof(stageType));
        }

        ArgumentNullException.ThrowIfNull(factory);

        var settingsType = factory.SettingsType;
        if (!typeof(StageSettingsBase).IsAssignableFrom(settingsType))
        {
            throw new InvalidOperationException(
                $"Factory '{factory.GetType().Name}' has unsupported settings type '{settingsType.Name}'.");
        }

        _stageSettingsTypes.Add(stageType, settingsType);

        if (_registeredFactorySettingsTypes.Add(settingsType))
        {
            _registry.Register(factory);
        }
    }
}
