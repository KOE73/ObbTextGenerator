namespace ObbTextGenerator;

public abstract class RenderStageSettingsBase : StageSettingsBase
{
    public RenderWindowSettings Window { get; init; } = new();
}
