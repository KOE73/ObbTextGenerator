using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace ObbTextGenerator;

internal class Program
{
    private const string DefaultConfigPath = "config.yaml";
    private const string DefaultFontGroupsSampleText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789 Example Пример Яя";
    private const string DefaultFontGroupsOutput = "LocalArtifacts/FontGroupPreview";
    private const string DefaultFontPreviewVariants = "family-only";
    private const string DefaultAppConfigPath = "app.config";

    static async Task<int> Main(string[] args)
    {
        var configFileOption = new Option<string>("--config-file");
        configFileOption.Description = "Path to the configuration file. Defaults to config.yaml.";
        configFileOption.DefaultValueFactory = _ => DefaultConfigPath;

        var fontGroupsOption = new Option<bool>("--font-groups");
        fontGroupsOption.Description = "Generate preview images for all built-in font groups.";

        var fontGroupsTextOption = new Option<string>("--font-groups-text");
        fontGroupsTextOption.Description = "Sample text used in font group preview images.";
        fontGroupsTextOption.DefaultValueFactory = _ => DefaultFontGroupsSampleText;

        var fontGroupsOutputOption = new Option<string>("--font-groups-output");
        fontGroupsOutputOption.Description = "Output directory for generated font group preview images.";

        var fontPreviewVariantsOption = new Option<string>("--font-preview-variants");
        fontPreviewVariantsOption.Description = "Preview mode for font variants: family-only, all, filtered.";
        fontPreviewVariantsOption.DefaultValueFactory = _ => DefaultFontPreviewVariants;
        fontPreviewVariantsOption.AcceptOnlyFromAmong("family-only", "all", "filtered");

        var maxParallelismOption = new Option<int?>("--max-parallelism");
        maxParallelismOption.Description = "Maximum number of samples generated in parallel. Use 0 for automatic CPU-based selection.";

        var rootCommand = new RootCommand("ObbTextGenerator");
        rootCommand.Add(configFileOption);
        rootCommand.Add(fontGroupsOption);
        rootCommand.Add(fontGroupsTextOption);
        rootCommand.Add(fontGroupsOutputOption);
        rootCommand.Add(fontPreviewVariantsOption);
        rootCommand.Add(maxParallelismOption);

        rootCommand.SetAction(parseResult =>
        {
            var configFile = parseResult.GetValue(configFileOption) ?? DefaultConfigPath;
            var fontGroups = parseResult.GetValue(fontGroupsOption);
            var fontGroupsText = parseResult.GetValue(fontGroupsTextOption) ?? DefaultFontGroupsSampleText;
            var fontGroupsOutput = parseResult.GetValue(fontGroupsOutputOption);
            var fontPreviewVariants = parseResult.GetValue(fontPreviewVariantsOption) ?? DefaultFontPreviewVariants;
            var maxParallelism = parseResult.GetValue(maxParallelismOption);

            if (fontGroups)
            {
                RunFontGroupsPreview(configFile, fontGroupsText, fontGroupsOutput, fontPreviewVariants);
                return;
            }

            RunGeneratorPipeline(configFile, maxParallelism);
        });

        var invocationConfiguration = new InvocationConfiguration();
        var parse = rootCommand.Parse(args);
        return await parse.InvokeAsync(invocationConfiguration);
    }

    private static void RunGeneratorPipeline(string configPath, int? maxParallelismOverride)
    {
        WriteBanner("Pipeline Initialization");

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[red][bold]Error:[/] Config file not found: [white]{configPath}[/][/]");
            return;
        }

        try
        {
            var stageRegistrations = BuildStageRegistrations();
            var fullConfig = ConfigurationLoader.Load(configPath, stageRegistrations.StageSettingsTypes);
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? string.Empty;
            fullConfig = ResolveRuntimePaths(fullConfig, configDirectory);
            var general = fullConfig.General;
            var resolvedMaxParallelism = maxParallelismOverride ?? general.MaxParallelism;

            AnsiConsole.MarkupLine($"[blue]Info:[/] Config: [white]{Path.GetFullPath(configPath)}[/]");
            AnsiConsole.MarkupLine($"[blue]Info:[/] Project Root: [yellow]{general.OutputRoot}[/]");
            AnsiConsole.MarkupLine($"[blue]Info:[/] Resource Root: [yellow]{general.ResourceRoot}[/]");
            AnsiConsole.MarkupLine($"[blue]Info:[/] Resolution: [green]{general.Width}x{general.Height}[/] | Samples: [green]{general.SampleCount}[/] | Split: [green]{general.TrainSplit:P0}[/] Train");
            AnsiConsole.MarkupLine($"[blue]Info:[/] Max Parallelism: [green]{(resolvedMaxParallelism <= 0 ? "auto" : resolvedMaxParallelism)}[/]");

            var registry = stageRegistrations.Registry;

            var renderSettings = new RenderSettings(general.Width, general.Height);
            var factoryContext = new StageFactoryContext
            {
                RenderSettings = renderSettings,
                FullConfig = fullConfig,
                StageRegistry = registry,
                ConfigDirectory = configDirectory,
                OutputRoot = general.OutputRoot,
                ResourceRoot = general.ResourceRoot
            };

            var pipeline = CreatePipeline(fullConfig, registry, factoryContext);
            if (pipeline.Count > 0)
            {
                var runner = new PipelineRunner(general, pipeline, resolvedMaxParallelism);
                runner.Run();
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    private static void RunFontGroupsPreview(string configPath, string sampleText, string? outputDirectory, string variantsModeText)
    {
        WriteBanner("Font Groups Preview");

        try
        {
            var stageRegistrations = BuildStageRegistrations();
            var configDirectory = GetConfigDirectoryForResources(configPath);
            var resourceRoot = GetResourceRootForPreview(configPath, stageRegistrations, configDirectory);
            var variantsMode = ParseFontPreviewVariantsMode(variantsModeText);
            var resolvedOutputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
                ? DefaultFontGroupsOutput
                : outputDirectory;

            var fullOutputDirectory = Path.IsPathRooted(resolvedOutputDirectory)
                ? resolvedOutputDirectory
                : Path.Combine(configDirectory, resolvedOutputDirectory);

            AnsiConsole.MarkupLine($"[blue]Info:[/] Config base: [white]{configDirectory}[/]");
            AnsiConsole.MarkupLine($"[blue]Info:[/] Resource Root: [yellow]{resourceRoot}[/]");
            AnsiConsole.MarkupLine($"[blue]Info:[/] Preview mode: [white]{variantsMode}[/]");
            RandomSystemFontProviderSettings? filteredSettings = null;
            if (variantsMode == FontPreviewVariantsMode.Filtered && File.Exists(configPath))
            {
                var fullConfig = ConfigurationLoader.Load(configPath, stageRegistrations.StageSettingsTypes);
                filteredSettings = FindFirstRandomSystemFontSettings(fullConfig);
            }

            var previewGenerator = new FontGroupPreviewGenerator();
            previewGenerator.GenerateAll(resourceRoot, fullOutputDirectory, sampleText, variantsMode, filteredSettings);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }

    private static List<IPipelineStage> CreatePipeline(
        FullConfig fullConfig,
        PipelineStageRegistry registry,
        StageFactoryContext factoryContext)
    {
        var pipeline = new List<IPipelineStage>();

        var table = new Table().Border(TableBorder.Rounded).Title("Pipeline Stages");
        table.AddColumn(new TableColumn("[bold]#[/]").RightAligned());
        table.AddColumn("[bold]Stage Name[/]");
        table.AddColumn("[bold]Settings Type[/]");
        table.AddColumn("[bold]Status[/]");

        var order = 1;
        foreach (var stageSettings in fullConfig.Stages)
        {
            try
            {
                var stage = registry.Create(stageSettings, factoryContext);
                pipeline.Add(stage);
                table.AddRow(
                    order++.ToString(),
                    $"[cyan]{stage.Name}[/]",
                    $"[yellow]{stageSettings.GetType().Name}[/]",
                    "[green]Success[/]");
            }
            catch (Exception ex)
            {
                table.AddRow(
                    order++.ToString(),
                    "[grey]-[/]",
                    $"[red]{stageSettings.GetType().Name}[/]",
                    $"[red]Error: {ex.Message}[/]");
            }
        }

        AnsiConsole.Write(table);

        if (pipeline.Count > 0)
        {
            AnsiConsole.MarkupLine($"[bold green]Status:[/] Pipeline initialized with [white]{pipeline.Count}[/] stages.");
            AnsiConsole.MarkupLine("[bold blue]Ready for synthetic data generation.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[bold yellow]Warning:[/] Pipeline is empty. Check your configuration.");
        }

        AnsiConsole.Write(new Rule().RuleStyle("grey"));
        return pipeline;
    }

    private static string GetConfigDirectoryForResources(string configPath)
    {
        if (File.Exists(configPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Directory.GetCurrentDirectory();
        }

        return Directory.GetCurrentDirectory();
    }

    private static FullConfig ResolveRuntimePaths(FullConfig fullConfig, string configDirectory)
    {
        var general = fullConfig.General;
        var resolvedOutputRoot = ResolvePathFromConfigDirectory(general.OutputRoot, configDirectory);
        var resolvedResourceRoot = ResolvePathFromConfigDirectory(general.ResourceRoot, configDirectory);

        var resolvedGeneral = new GenerationSettings
        {
            Width = general.Width,
            Height = general.Height,
            OutputRoot = resolvedOutputRoot,
            ResourceRoot = resolvedResourceRoot,
            SampleCount = general.SampleCount,
            TrainSplit = general.TrainSplit,
            MaxParallelism = general.MaxParallelism
        };

        return new FullConfig
        {
            General = resolvedGeneral,
            Stages = fullConfig.Stages,
            PipelinePrograms = fullConfig.PipelinePrograms,
            Schemes = fullConfig.Schemes,
            Patterns = fullConfig.Patterns
        };
    }

    private static string ResolvePathFromConfigDirectory(string path, string configDirectory)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        return Path.GetFullPath(Path.Combine(configDirectory, path));
    }

    private static string GetResourceRootForPreview(
        string configPath,
        PipelineStageRegistrationCollection stageRegistrations,
        string configDirectory)
    {
        if (!File.Exists(configPath))
        {
            return ResolvePathFromConfigDirectory("Resources", configDirectory);
        }

        var fullConfig = ConfigurationLoader.Load(configPath, stageRegistrations.StageSettingsTypes);
        return ResolvePathFromConfigDirectory(fullConfig.General.ResourceRoot, configDirectory);
    }

    private static FontPreviewVariantsMode ParseFontPreviewVariantsMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "all" => FontPreviewVariantsMode.All,
            "filtered" => FontPreviewVariantsMode.Filtered,
            _ => FontPreviewVariantsMode.FamilyOnly
        };
    }

    private static RandomSystemFontProviderSettings? FindFirstRandomSystemFontSettings(FullConfig fullConfig)
    {
        var stageFontSettings = FindFirstRandomSystemFontSettings(fullConfig.Stages);
        if (stageFontSettings is not null)
        {
            return stageFontSettings;
        }

        foreach (var program in fullConfig.PipelinePrograms)
        {
            var programFontSettings = FindFirstRandomSystemFontSettings(program.Stages);
            if (programFontSettings is not null)
            {
                return programFontSettings;
            }
        }

        return null;
    }

    private static RandomSystemFontProviderSettings? FindFirstRandomSystemFontSettings(IReadOnlyList<StageSettingsBase> stages)
    {
        foreach (var stage in stages)
        {
            if (stage is TextLineStageSettings textLineStageSettings
                && textLineStageSettings.Font is RandomSystemFontProviderSettings randomSystemFontProviderSettings)
            {
                return randomSystemFontProviderSettings;
            }

            var nestedSettings = GetNestedStageSettings(stage);
            if (nestedSettings.Count == 0)
            {
                continue;
            }

            var nestedFontSettings = FindFirstRandomSystemFontSettings(nestedSettings);
            if (nestedFontSettings is not null)
            {
                return nestedFontSettings;
            }
        }

        return null;
    }

    private static IReadOnlyList<StageSettingsBase> GetNestedStageSettings(StageSettingsBase stage)
    {
        return stage switch
        {
            PipelineBlockStageSettings blockSettings => blockSettings.Stages,
            PipelineRepeatStageSettings repeatSettings => repeatSettings.Stages,
            PipelineProgramStageSettings => [],
            PipelineSelectStageSettings => [],
            _ => []
        };
    }

    private static void WriteBanner(string ruleTitle)
    {
        AnsiConsole.Write(
            new FigletText("OBB Gen")
                .Color(Color.DeepSkyBlue1));

        AnsiConsole.Write(new Rule($"[yellow]{ruleTitle}[/]"));
        AnsiConsole.WriteLine();
    }

    private static PipelineStageRegistrationCollection BuildStageRegistrations()
    {
        var registrations = new PipelineStageRegistrationCollection();
        BuiltInPipelineStages.RegisterAll(registrations);

        var appConfigPath = Path.Combine(AppContext.BaseDirectory, DefaultAppConfigPath);
        var loadedModuleNames = PipelineStageModuleLoader.LoadFromAppConfig(appConfigPath, registrations);

        foreach (var loadedModuleName in loadedModuleNames)
        {
            AnsiConsole.MarkupLine($"[blue]Info:[/] Loaded stage module: [white]{loadedModuleName}[/]");
        }

        return registrations;
    }
}
