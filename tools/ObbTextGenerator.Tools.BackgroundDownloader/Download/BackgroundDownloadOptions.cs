using System.IO;

namespace ObbTextGenerator;

public sealed class BackgroundDownloadOptions
{
    public required string OutputDirectory { get; init; }

    public required string ManifestPath { get; init; }

    public required int Count { get; init; }

    public required string Query { get; init; }

    public required string Categories { get; init; }

    public required string Purity { get; init; }

    public required string Sorting { get; init; }

    public required string Order { get; init; }

    public required string TopRange { get; init; }

    public required string AtLeast { get; init; }

    public required string Resolutions { get; init; }

    public required string Ratios { get; init; }

    public required string Colors { get; init; }

    public required string Seed { get; init; }

    public required string ApiKey { get; init; }

    public required int StartPage { get; init; }

    public required int MaxPages { get; init; }

    public required int ApiDelayMilliseconds { get; init; }

    public required int DownloadDelayMilliseconds { get; init; }

    public required int TimeoutSeconds { get; init; }

    public required bool Overwrite { get; init; }

    public required bool DryRun { get; init; }

    public required string FileNamePattern { get; init; }

    public required string UserAgent { get; init; }

    public static BackgroundDownloadOptions FromSettings(DownloadBackgroundsSettings settings, string apiKey)
    {
        var outputDirectory = Path.GetFullPath(settings.OutputDirectory);
        var manifestPath = Path.Combine(outputDirectory, settings.ManifestFileName);

        return new BackgroundDownloadOptions
        {
            OutputDirectory = outputDirectory,
            ManifestPath = manifestPath,
            Count = settings.Count,
            Query = settings.Query,
            Categories = settings.Categories,
            Purity = settings.Purity,
            Sorting = settings.Sorting,
            Order = settings.Order,
            TopRange = settings.TopRange,
            AtLeast = settings.AtLeast,
            Resolutions = settings.Resolutions,
            Ratios = settings.Ratios,
            Colors = settings.Colors,
            Seed = settings.Seed,
            ApiKey = apiKey,
            StartPage = settings.StartPage,
            MaxPages = settings.MaxPages,
            ApiDelayMilliseconds = settings.ApiDelayMilliseconds,
            DownloadDelayMilliseconds = settings.DownloadDelayMilliseconds,
            TimeoutSeconds = settings.TimeoutSeconds,
            Overwrite = settings.Overwrite,
            DryRun = settings.DryRun,
            FileNamePattern = settings.FileNamePattern,
            UserAgent = settings.UserAgent
        };
    }
}
