using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace ObbTextGenerator;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public sealed class DownloadBackgroundsSettings : CommandSettings
{
    [CommandOption("--source <SOURCE>")]
    [Description("Image source API. Currently supported: wallhaven.")]
    [DefaultValue("wallhaven")]
    public string Source { get; init; } = "wallhaven";

    [CommandOption("-o|--output <DIR>")]
    [Description("Output directory for downloaded backgrounds.")]
    [DefaultValue("LocalArtifacts/Backgrounds/Wallhaven")]
    public string OutputDirectory { get; init; } = "LocalArtifacts/Backgrounds/Wallhaven";

    [CommandOption("-c|--count <COUNT>")]
    [Description("Maximum number of images to download.")]
    [DefaultValue(100)]
    public int Count { get; init; } = 100;

    [CommandOption("-q|--query <QUERY>")]
    [Description("Wallhaven search query. Examples: fabric, +paper -anime, type:jpg.")]
    [DefaultValue("")]
    public string Query { get; init; } = string.Empty;

    [CommandOption("--categories <VALUE>")]
    [Description("Wallhaven categories bitmask: general/anime/people. Example: 100, 101, 111.")]
    [DefaultValue("100")]
    public string Categories { get; init; } = "100";

    [CommandOption("--purity <VALUE>")]
    [Description("Wallhaven purity bitmask: sfw/sketchy/nsfw. NSFW requires an API key. Example: 100, 110, 111.")]
    [DefaultValue("100")]
    public string Purity { get; init; } = "100";

    [CommandOption("--sorting <VALUE>")]
    [Description("Wallhaven sorting: date_added, relevance, random, views, favorites, toplist.")]
    [DefaultValue("toplist")]
    public string Sorting { get; init; } = "toplist";

    [CommandOption("--order <VALUE>")]
    [Description("Wallhaven order: desc or asc.")]
    [DefaultValue("desc")]
    public string Order { get; init; } = "desc";

    [CommandOption("--top-range <VALUE>")]
    [Description("Wallhaven toplist range: 1d, 3d, 1w, 1M, 3M, 6M, 1y.")]
    [DefaultValue("1M")]
    public string TopRange { get; init; } = "1M";

    [CommandOption("--atleast <RESOLUTION>")]
    [Description("Minimum resolution. Example: 1024x1024, 1920x1080.")]
    [DefaultValue("1024x1024")]
    public string AtLeast { get; init; } = "1024x1024";

    [CommandOption("--resolutions <VALUE>")]
    [Description("Exact resolution filter. Example: 1920x1080,2560x1440.")]
    [DefaultValue("")]
    public string Resolutions { get; init; } = string.Empty;

    [CommandOption("--ratios <VALUE>")]
    [Description("Aspect ratio filter. Example: 16x9,16x10.")]
    [DefaultValue("")]
    public string Ratios { get; init; } = string.Empty;

    [CommandOption("--colors <VALUE>")]
    [Description("Wallhaven color filter hex without '#'.")]
    [DefaultValue("")]
    public string Colors { get; init; } = string.Empty;

    [CommandOption("--seed <VALUE>")]
    [Description("Seed for random sorting pagination.")]
    [DefaultValue("")]
    public string Seed { get; init; } = string.Empty;

    [CommandOption("--api-key <KEY>")]
    [Description("Wallhaven API key. If omitted, WALLHAVEN_API_KEY is used.")]
    [DefaultValue("")]
    public string ApiKey { get; init; } = string.Empty;

    [CommandOption("--start-page <PAGE>")]
    [Description("First Wallhaven search result page.")]
    [DefaultValue(1)]
    public int StartPage { get; init; } = 1;

    [CommandOption("--max-pages <COUNT>")]
    [Description("Maximum pages to scan. Use 0 for no explicit limit.")]
    [DefaultValue(0)]
    public int MaxPages { get; init; }

    [CommandOption("--api-delay-ms <MS>")]
    [Description("Delay between Wallhaven API page requests.")]
    [DefaultValue(1400)]
    public int ApiDelayMilliseconds { get; init; } = 1400;

    [CommandOption("--download-delay-ms <MS>")]
    [Description("Delay between image downloads.")]
    [DefaultValue(200)]
    public int DownloadDelayMilliseconds { get; init; } = 200;

    [CommandOption("--timeout-seconds <SECONDS>")]
    [Description("HTTP timeout in seconds.")]
    [DefaultValue(30)]
    public int TimeoutSeconds { get; init; } = 30;

    [CommandOption("--overwrite")]
    [Description("Overwrite existing files with the same generated name.")]
    public bool Overwrite { get; init; }

    [CommandOption("--dry-run")]
    [Description("Scan API pages and show selected entries without downloading files.")]
    public bool DryRun { get; init; }

    [CommandOption("--file-name-pattern <PATTERN>")]
    [Description("Output file name pattern. Supported tokens: {index}, {id}, {width}, {height}.")]
    [DefaultValue("bg_{index}_{id}")]
    public string FileNamePattern { get; init; } = "bg_{index}_{id}";

    [CommandOption("--manifest <FILE>")]
    [Description("Manifest file name written inside the output directory.")]
    [DefaultValue("manifest.json")]
    public string ManifestFileName { get; init; } = "manifest.json";

    [CommandOption("--user-agent <VALUE>")]
    [Description("HTTP user agent.")]
    [DefaultValue("ObbTextGenerator.BackgroundDownloader/1.0")]
    public string UserAgent { get; init; } = "ObbTextGenerator.BackgroundDownloader/1.0";

    public override Spectre.Console.ValidationResult Validate()
    {
        if (Count <= 0)
        {
            return Spectre.Console.ValidationResult.Error("--count must be greater than 0.");
        }

        if (StartPage <= 0)
        {
            return Spectre.Console.ValidationResult.Error("--start-page must be greater than 0.");
        }

        if (ApiDelayMilliseconds < 0 || DownloadDelayMilliseconds < 0)
        {
            return Spectre.Console.ValidationResult.Error("Delay values must be greater than or equal to 0.");
        }

        if (TimeoutSeconds <= 0)
        {
            return Spectre.Console.ValidationResult.Error("--timeout-seconds must be greater than 0.");
        }

        if (!string.Equals(Source, "wallhaven", StringComparison.OrdinalIgnoreCase))
        {
            return Spectre.Console.ValidationResult.Error("Only the wallhaven source is currently supported.");
        }

        return Spectre.Console.ValidationResult.Success();
    }
}
