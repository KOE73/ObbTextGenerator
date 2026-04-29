using System.Reflection;
using System.Runtime.Loader;

namespace ObbTextGenerator;

public static class PipelineStageModuleLoader
{
    private static readonly List<AssemblyDependencyResolver> DependencyResolvers = [];
    private static bool isAssemblyResolutionRegistered;

    public static List<string> LoadFromAppConfig(
        string appConfigPath,
        PipelineStageRegistrationCollection registrations)
    {
        var loadedModuleNames = new List<string>();

        if (!File.Exists(appConfigPath))
        {
            return loadedModuleNames;
        }

        var appConfig = AppConfigLoader.Load(appConfigPath);
        var appConfigDirectory = Path.GetDirectoryName(Path.GetFullPath(appConfigPath)) ?? string.Empty;

        foreach (var modulePath in appConfig.StageModulePaths)
        {
            var resolvedModulePath = modulePath;
            if (!Path.IsPathRooted(resolvedModulePath))
            {
                resolvedModulePath = Path.Combine(appConfigDirectory, resolvedModulePath);
            }

            if (!File.Exists(resolvedModulePath))
            {
                throw new FileNotFoundException(
                    $"Stage module assembly not found: '{resolvedModulePath}'.",
                    resolvedModulePath);
            }

            RegisterAssemblyResolverIfNeeded();
            DependencyResolvers.Add(new AssemblyDependencyResolver(resolvedModulePath));

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(resolvedModulePath));
            var moduleTypes = assembly
                .GetTypes()
                .Where(type => typeof(IPipelineStageModule).IsAssignableFrom(type))
                .Where(type => !type.IsAbstract)
                .Where(type => !type.IsInterface)
                .ToList();

            if (moduleTypes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Assembly '{resolvedModulePath}' does not contain any '{nameof(IPipelineStageModule)}' implementation.");
            }

            foreach (var moduleType in moduleTypes)
            {
                if (Activator.CreateInstance(moduleType) is not IPipelineStageModule module)
                {
                    throw new InvalidOperationException(
                        $"Failed to create stage module '{moduleType.FullName}'.");
                }

                module.RegisterStages(registrations);
                loadedModuleNames.Add(module.Name);
            }
        }

        return loadedModuleNames;
    }

    private static void RegisterAssemblyResolverIfNeeded()
    {
        if (isAssemblyResolutionRegistered)
        {
            return;
        }

        AssemblyLoadContext.Default.Resolving += ResolvePluginAssembly;
        isAssemblyResolutionRegistered = true;
    }

    private static Assembly? ResolvePluginAssembly(AssemblyLoadContext loadContext, AssemblyName assemblyName)
    {
        foreach (var dependencyResolver in DependencyResolvers)
        {
            var assemblyPath = dependencyResolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
            {
                return loadContext.LoadFromAssemblyPath(assemblyPath);
            }
        }

        var applicationAssemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(applicationAssemblyPath))
        {
            return loadContext.LoadFromAssemblyPath(applicationAssemblyPath);
        }

        return null;
    }
}
