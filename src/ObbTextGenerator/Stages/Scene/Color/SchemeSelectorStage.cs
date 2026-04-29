namespace ObbTextGenerator;

public sealed class SchemeSelectorStage(SchemeSelectorStageSettings settings, StageFactoryContext stageFactoryContext) : IPipelineStage
{
    private readonly SchemeSelectorStageSettings _settings = settings;
    private readonly StageFactoryContext _stageFactoryContext = stageFactoryContext;
    private readonly FullConfig _fullConfig = stageFactoryContext.FullConfig;

    public string Name => "Color/SchemeSelector";

    public void Apply(RenderContext context)
    {
        if (_fullConfig.Schemes.Count == 0) return;

        ColorSchemeSettings? scheme = null;
        if (!string.IsNullOrEmpty(_settings.SchemeName))
        {
            scheme = _fullConfig.Schemes.FirstOrDefault(s => s.Name.Equals(_settings.SchemeName, StringComparison.OrdinalIgnoreCase));
        }

        scheme ??= _fullConfig.Schemes[context.Settings.Random.Next(_fullConfig.Schemes.Count)];
        context.ActiveScheme = scheme;
        context.AddTrace($"scheme-selector: {scheme.Name}");
    }
}

public sealed class SchemeSelectorStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(SchemeSelectorStageSettings);
    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new SchemeSelectorStage((SchemeSelectorStageSettings)settings, context);
    }
}
