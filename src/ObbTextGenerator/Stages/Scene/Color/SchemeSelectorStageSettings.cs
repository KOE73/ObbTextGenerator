namespace ObbTextGenerator;

public sealed class SchemeSelectorStageSettings : StageSettingsBase
{
    /// <summary>
    /// If specified, only this scheme will be used. 
    /// If null, a random scheme from global settings will be picked.
    /// </summary>
    public string? SchemeName { get; init; }
}
