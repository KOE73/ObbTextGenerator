namespace ObbTextGenerator;

public interface IPipelineStageModule
{
    string Name { get; }

    void RegisterStages(PipelineStageRegistrationCollection registrations);
}
