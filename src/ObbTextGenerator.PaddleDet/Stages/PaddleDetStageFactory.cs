namespace ObbTextGenerator;

/// <summary>
/// Factory for the PaddleDet stage.
/// Keeps plugin registration consistent with the main pipeline factory model.
/// </summary>
public sealed class PaddleDetStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(PaddleDetStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        return new PaddleDetStage((PaddleDetStageSettings)settings, context.ConfigDirectory);
    }
}
