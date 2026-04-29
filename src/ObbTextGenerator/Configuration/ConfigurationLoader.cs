using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ObbTextGenerator;

/// <summary>
/// Utility class for loading generator configuration from YAML files.
/// </summary>
public static class ConfigurationLoader
{
    /// <summary>
    /// Loads the full generator configuration from a YAML file.
    /// </summary>
    /// <param name="yamlPath">Path to the .yaml configuration file.</param>
    /// <returns>The loaded full configuration.</returns>
    public static FullConfig Load(string yamlPath, IReadOnlyDictionary<string, Type> stageSettingsTypes)
    {
        if (!File.Exists(yamlPath))
            throw new FileNotFoundException("Configuration file not found.", yamlPath);

        var input = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new SampledValueSpecYamlTypeConverter())
                    .WithTypeDiscriminatingNodeDeserializer(options =>
                    {
                        options.AddKeyValueTypeDiscriminator<StageSettingsBase>("type", new Dictionary<string, Type>(stageSettingsTypes, StringComparer.OrdinalIgnoreCase));

                        options.AddKeyValueTypeDiscriminator<TextProviderSettingsBase>("type", new Dictionary<string, Type>
                        {
                            ["constant"] = typeof(ConstantTextProviderSettings),
                            ["random-char"] = typeof(RandomCharProviderSettings),
                            ["pattern"] = typeof(PatternTextProviderSettings),
                            ["file-lines"] = typeof(FileLinesProviderSettings),
                            ["composite"] = typeof(WeightedCompositeProviderSettings),
                        });

                        options.AddKeyValueTypeDiscriminator<FontProviderSettingsBase>("type", new Dictionary<string, Type>
                        {
                            ["constant"] = typeof(ConstantFontProviderSettings),
                            ["system-random"] = typeof(RandomSystemFontProviderSettings),
                        });

                        options.AddKeyValueTypeDiscriminator<ColorProviderSettingsBase>("type", new Dictionary<string, Type>
                        {
                            ["constant"] = typeof(ConstantColorProviderSettings),
                            ["random"] = typeof(RandomColorProviderSettings),
                            ["gray"] = typeof(GrayColorProviderSettings),
                            ["from-scheme"] = typeof(FromSchemeColorProviderSettings),
                        });

                        options.AddKeyValueTypeDiscriminator<PatternLayerSettingsBase>("type", new Dictionary<string, Type>
                        {
                            ["fill"] = typeof(FillLayerSettings),
                            ["rect-fixed"] = typeof(RectFixedLayerSettings),
                            ["rect-random"] = typeof(RectRandomLayerSettings),
                            ["circle-random"] = typeof(CircleRandomLayerSettings),
                            ["line-random"] = typeof(LineRandomLayerSettings),
                            ["scatter-random"] = typeof(ScatterRandomLayerSettings),
                            ["grid-procedural"] = typeof(GridProceduralLayerSettings),
                            ["perlin-procedural"] = typeof(PerlinProceduralLayerSettings),
                        });
                    })
            .Build();

        var config = deserializer.Deserialize<FullConfig>(input);
        return config ?? new FullConfig();
    }
}
