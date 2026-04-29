namespace ObbTextGenerator;

/// <summary>
/// Factory for creating <see cref="TextLineStage"/> from configuration.
/// </summary>
public sealed class TextLineStageFactory : IPipelineStageFactory
{
    /// <inheritdoc />
    public Type SettingsType => typeof(TextLineStageSettings);

    /// <inheritdoc />
    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var s = (TextLineStageSettings)settings;
        var textProvider = TextProviderFactory.Create(s.Provider, context);
        var fontProvider = FontProviderFactory.Create(s.Font, context);
        var colorProvider = ColorProviderFactory.Create(s.Color, context);
        return new TextLineStage(textProvider, fontProvider, colorProvider, s);
    }
}
