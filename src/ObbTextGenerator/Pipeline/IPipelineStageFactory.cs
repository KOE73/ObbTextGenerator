namespace ObbTextGenerator;     

public interface IPipelineStageFactory
{
    /// <summary>
    /// Type of the settings object this factory handles.
    /// </summary>
    Type SettingsType { get; }

    IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context);
}
