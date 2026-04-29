using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ObbTextGenerator;

public static class FontGroupLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static HashSet<string> LoadFamilies(IReadOnlyList<string> groupNames, string resourceRoot)
    {
        var families = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupName in groupNames)
        {
            var definition = LoadGroup(groupName, resourceRoot);
            foreach (var family in definition.Families)
            {
                if (string.IsNullOrWhiteSpace(family))
                {
                    continue;
                }

                families.Add(family.Trim());
            }
        }

        return families;
    }

    public static List<FontGroupDefinition> LoadAllGroups(string resourceRoot)
    {
        var definitions = new List<FontGroupDefinition>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredDirectory = Path.Combine(resourceRoot, "FontGroups");
        var candidateDirectories = Directory.Exists(configuredDirectory)
            ? [configuredDirectory]
            : GetCandidateDirectories(resourceRoot);

        foreach (var candidateDirectory in candidateDirectories)
        {
            if (!Directory.Exists(candidateDirectory))
            {
                continue;
            }

            var groupPaths = Directory.GetFiles(candidateDirectory, "*.yaml", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var groupPath in groupPaths)
            {
                var groupName = Path.GetFileNameWithoutExtension(groupPath);
                if (!seenNames.Add(groupName))
                {
                    continue;
                }

                var definition = LoadGroup(groupName, resourceRoot);
                definitions.Add(definition);
            }
        }

        return definitions;
    }

    private static FontGroupDefinition LoadGroup(string groupName, string resourceRoot)
    {
        var candidatePaths = GetCandidatePaths(groupName, resourceRoot);
        foreach (var candidatePath in candidatePaths)
        {
            if (!File.Exists(candidatePath))
            {
                continue;
            }

            var yaml = File.ReadAllText(candidatePath);
            var definition = Deserializer.Deserialize<FontGroupDefinition>(yaml) ?? new FontGroupDefinition();
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                definition = new FontGroupDefinition
                {
                    Name = groupName,
                    Families = definition.Families
                };
            }

            return definition;
        }

        var message = $"Font group '{groupName}' was not found. Searched: {string.Join(", ", candidatePaths)}";
        throw new FileNotFoundException(message, candidatePaths[0]);
    }

    private static List<string> GetCandidatePaths(string groupName, string resourceRoot)
    {
        var fileName = groupName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            ? groupName
            : $"{groupName}.yaml";

        var candidatePaths = new List<string>();

        if (Path.IsPathRooted(fileName))
        {
            candidatePaths.Add(fileName);
            return candidatePaths;
        }

        foreach (var candidateDirectory in GetCandidateDirectories(resourceRoot))
        {
            var candidatePath = Path.Combine(candidateDirectory, fileName);
            if (!candidatePaths.Contains(candidatePath, StringComparer.OrdinalIgnoreCase))
            {
                candidatePaths.Add(candidatePath);
            }
        }

        return candidatePaths;
    }

    private static List<string> GetCandidateDirectories(string resourceRoot)
    {
        var candidateDirectories = new List<string>();

        var configuredDirectory = Path.Combine(resourceRoot, "FontGroups");
        candidateDirectories.Add(configuredDirectory);

        var outputRelativeDirectory = Path.Combine(AppContext.BaseDirectory, "Resources", "FontGroups");
        if (!candidateDirectories.Contains(outputRelativeDirectory, StringComparer.OrdinalIgnoreCase))
        {
            candidateDirectories.Add(outputRelativeDirectory);
        }

        return candidateDirectories;
    }
}
