using SkiaSharp;

namespace ObbTextGenerator;

/// <summary>
/// Factory for creating <see cref="BackgroundFillColorStage"/> from configuration.
/// </summary>
public sealed class BackgroundFillColorStageFactory : IPipelineStageFactory
{
    /// <inheritdoc />
    public Type SettingsType => typeof(BackgroundFillColorStageSettings);

    /// <inheritdoc />
    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var s = (BackgroundFillColorStageSettings)settings;
        var colorProvider = ColorProviderFactory.Create(s.Color, context);
        return new BackgroundFillColorStage(s, colorProvider);
    }
}
