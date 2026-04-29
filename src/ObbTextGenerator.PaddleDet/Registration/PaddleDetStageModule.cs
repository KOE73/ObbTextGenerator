namespace ObbTextGenerator;

/// <summary>
/// Plugin registrar for PaddleDet-related pipeline stages.
/// </summary>
public sealed class PaddleDetStageModule : IPipelineStageModule
{
    public string Name => "ObbTextGenerator.PaddleDet";

    public void RegisterStages(PipelineStageRegistrationCollection registrations)
    {
        registrations.Register("paddledet", new PaddleDetStageFactory());
    }
}
