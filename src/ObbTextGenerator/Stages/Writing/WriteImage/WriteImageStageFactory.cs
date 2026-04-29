namespace ObbTextGenerator;

public sealed class WriteImageStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(WriteImageStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var imageSettings = (WriteImageStageSettings)settings;
        return new WriteImageStage(imageSettings.Path, imageSettings.Format, imageSettings.Quality, context.OutputRoot);
    }
}
