using Spectre.Console;
using Spectre.Console.Cli;

namespace ObbTextGenerator;

public sealed class DownloadBackgroundsCommand : AsyncCommand<DownloadBackgroundsSettings>
{
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        DownloadBackgroundsSettings settings,
        CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey(settings);
        var options = BackgroundDownloadOptions.FromSettings(settings, apiKey);

        Directory.CreateDirectory(options.OutputDirectory);

        WriteRunSummary(options);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);

        var apiClient = new WallhavenApiClient(httpClient, options);
        var manifestWriter = new BackgroundManifestWriter();
        var service = new BackgroundDownloadService(httpClient, apiClient, manifestWriter);

        try
        {
            var result = await service.DownloadAsync(options, cancellationToken);
            WriteResult(result, options);
            return result.FailedCount == 0 ? 0 : 2;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            return 1;
        }
    }

    private static string ResolveApiKey(DownloadBackgroundsSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return settings.ApiKey;
        }

        var environmentApiKey = Environment.GetEnvironmentVariable("WALLHAVEN_API_KEY");
        return environmentApiKey ?? string.Empty;
    }

    private static void WriteRunSummary(BackgroundDownloadOptions options)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("Download Plan");

        table.AddColumn("Setting");
        table.AddColumn("Value");
        table.AddRow("Source", "wallhaven");
        table.AddRow("Output", Markup.Escape(options.OutputDirectory));
        table.AddRow("Count", options.Count.ToString());
        table.AddRow("Query", string.IsNullOrWhiteSpace(options.Query) ? "[grey]<empty>[/]" : Markup.Escape(options.Query));
        table.AddRow("Categories", Markup.Escape(options.Categories));
        table.AddRow("Purity", Markup.Escape(options.Purity));
        table.AddRow("Sorting", Markup.Escape(options.Sorting));
        table.AddRow("At least", Markup.Escape(options.AtLeast));
        table.AddRow("Dry run", options.DryRun ? "[yellow]yes[/]" : "[green]no[/]");
        table.AddRow("API key", string.IsNullOrWhiteSpace(options.ApiKey) ? "[grey]not set[/]" : "[green]set[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void WriteResult(BackgroundDownloadResult result, BackgroundDownloadOptions options)
    {
        var summary = new Table()
            .Border(TableBorder.Rounded)
            .Title("Result");

        summary.AddColumn("Metric");
        summary.AddColumn("Value");
        summary.AddRow("Downloaded", result.DownloadedCount.ToString());
        summary.AddRow("Skipped", result.SkippedCount.ToString());
        summary.AddRow("Failed", result.FailedCount.ToString());
        summary.AddRow("Scanned pages", result.ScannedPages.ToString());
        summary.AddRow("Manifest", Markup.Escape(options.ManifestPath));

        AnsiConsole.Write(summary);

        if (result.FailedCount > 0)
        {
            AnsiConsole.MarkupLine("[yellow]Some downloads failed. Check the manifest and console log for details.[/]");
        }
    }
}
