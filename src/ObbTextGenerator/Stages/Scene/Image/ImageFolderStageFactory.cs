namespace ObbTextGenerator;

public sealed class ImageFolderStageFactory : IPipelineStageFactory
{
    public Type SettingsType => typeof(ImageFolderStageSettings);

    public IPipelineStage Create(StageSettingsBase settings, StageFactoryContext context)
    {
        var imageSettings = (ImageFolderStageSettings)settings;
        var fullPath = ResolveFullPath(imageSettings.Path, context.ConfigDirectory);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Image folder stage path was not found: {fullPath}");
        }

        var searchOption = imageSettings.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var imagePaths = new List<string>();

        foreach (var searchPattern in imageSettings.SearchPatterns)
        {
            imagePaths.AddRange(Directory.GetFiles(fullPath, searchPattern, searchOption));
        }

        imagePaths = imagePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (imagePaths.Count == 0)
        {
            throw new InvalidOperationException($"Image folder stage did not find any files in '{fullPath}'.");
        }

        return new ImageFolderStage(imageSettings, imagePaths);
    }

    private static string ResolveFullPath(string path, string configDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(configDirectory, path);
    }
}
