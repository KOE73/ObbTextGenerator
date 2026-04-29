namespace ObbTextGenerator;

public sealed class WriteImageStageSettings : StageSettingsBase
{
    public string Path { get; init; } = "images";
    public string Format { get; init; } = "png";
    public int Quality { get; init; } = 100;
}
