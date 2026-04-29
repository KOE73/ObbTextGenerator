namespace ObbTextGenerator;

public sealed class BackgroundFillColorStageSettings : RenderStageSettingsBase
{
    public ColorProviderSettingsBase Color { get; init; } = new ConstantColorProviderSettings { Color = "white" };
}
