using System.Xml.Linq;

namespace ObbTextGenerator;

public static class AppConfigLoader
{
    public static AppConfig Load(string appConfigPath)
    {
        var appConfig = new AppConfig();

        if (!File.Exists(appConfigPath))
        {
            return appConfig;
        }

        var document = XDocument.Load(appConfigPath);
        var rootElement = document.Root;
        if (rootElement is null)
        {
            return appConfig;
        }

        var stageModulesElement = rootElement.Element("stageModules");
        if (stageModulesElement is null)
        {
            return appConfig;
        }

        foreach (var addElement in stageModulesElement.Elements("add"))
        {
            var pathAttribute = addElement.Attribute("path");
            var modulePath = pathAttribute?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(modulePath))
            {
                continue;
            }

            appConfig.StageModulePaths.Add(modulePath);
        }

        return appConfig;
    }
}
