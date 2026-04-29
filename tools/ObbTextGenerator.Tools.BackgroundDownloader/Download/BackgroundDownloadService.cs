using Spectre.Console;

namespace ObbTextGenerator;

public sealed class BackgroundDownloadService
{
    private readonly HttpClient httpClient;
    private readonly WallhavenApiClient apiClient;
    private readonly BackgroundManifestWriter manifestWriter;

    public BackgroundDownloadService(
        HttpClient httpClient,
        WallhavenApiClient apiClient,
        BackgroundManifestWriter manifestWriter)
    {
        this.httpClient = httpClient;
        this.apiClient = apiClient;
        this.manifestWriter = manifestWriter;
    }

    public async Task<BackgroundDownloadResult> DownloadAsync(
        BackgroundDownloadOptions options,
        CancellationToken cancellationToken)
    {
        var manifest = manifestWriter.LoadOrCreate(options.ManifestPath);
        var result = new BackgroundDownloadResult();

        await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
            [
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
                new RemainingTimeColumn()
            ])
            .StartAsync(async progressContext =>
            {
                var totalTask = progressContext.AddTask("[green]total[/]", maxValue: options.Count);
                var pageTask = progressContext.AddTask("[blue]pages[/]", maxValue: ResolvePageTaskTotal(options));
                var fileTask = progressContext.AddTask("[yellow]current file[/]", maxValue: 1);

                await DownloadInternalAsync(options, manifest, result, totalTask, pageTask, fileTask, cancellationToken);
            });

        manifestWriter.Save(options.ManifestPath, manifest);
        return result;
    }

    private async Task DownloadInternalAsync(
        BackgroundDownloadOptions options,
        BackgroundManifest manifest,
        BackgroundDownloadResult result,
        ProgressTask totalTask,
        ProgressTask pageTask,
        ProgressTask fileTask,
        CancellationToken cancellationToken)
    {
        var page = options.StartPage;
        var selectedCount = 0;
        var pageLimit = ResolveLastPageToScan(options);

        while (selectedCount < options.Count)
        {
            if (pageLimit.HasValue && page > pageLimit.Value)
            {
                break;
            }

            pageTask.Description = $"[blue]page {page}[/]";

            var response = await apiClient.SearchAsync(page, cancellationToken);
            result.ScannedPages++;
            pageTask.Increment(1);

            if (response.Data.Count == 0)
            {
                break;
            }

            foreach (var wallpaper in response.Data)
            {
                if (selectedCount >= options.Count)
                {
                    break;
                }

                var entry = CreateManifestEntry(options, wallpaper, selectedCount);
                selectedCount++;

                if (options.DryRun)
                {
                    fileTask.Description = $"[yellow]dry-run: {Markup.Escape(entry.FileName)}[/]";
                    fileTask.Value = fileTask.MaxValue;
                    manifest.AddOrUpdate(entry);
                    result.SkippedCount++;
                    totalTask.Increment(1);
                    continue;
                }

                var downloaded = await DownloadWallpaperAsync(options, entry, fileTask, cancellationToken);
                manifest.AddOrUpdate(entry);

                if (downloaded == DownloadStatus.Downloaded)
                {
                    result.DownloadedCount++;
                }
                else if (downloaded == DownloadStatus.Skipped)
                {
                    result.SkippedCount++;
                }
                else
                {
                    result.FailedCount++;
                }

                totalTask.Increment(1);

                if (options.DownloadDelayMilliseconds > 0)
                {
                    await Task.Delay(options.DownloadDelayMilliseconds, cancellationToken);
                }
            }

            if (page >= response.Meta.LastPage)
            {
                break;
            }

            page++;

            if (options.ApiDelayMilliseconds > 0)
            {
                await Task.Delay(options.ApiDelayMilliseconds, cancellationToken);
            }
        }
    }

    private async Task<DownloadStatus> DownloadWallpaperAsync(
        BackgroundDownloadOptions options,
        BackgroundManifestEntry entry,
        ProgressTask fileTask,
        CancellationToken cancellationToken)
    {
        if (File.Exists(entry.LocalPath) && !options.Overwrite)
        {
            entry.Status = "skipped-existing";
            return DownloadStatus.Skipped;
        }

        try
        {
            fileTask.Value = 0;
            fileTask.Description = $"[yellow]{Markup.Escape(entry.FileName)}[/]";

            using var response = await httpClient.GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                entry.Status = $"failed-http-{(int)response.StatusCode}";
                return DownloadStatus.Failed;
            }

            var contentLength = response.Content.Headers.ContentLength;
            fileTask.MaxValue = contentLength.HasValue && contentLength.Value > 0 ? contentLength.Value : 1;

            await using var inputStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var outputStream = File.Create(entry.LocalPath);

            var buffer = new byte[128 * 1024];
            while (true)
            {
                var bytesRead = await inputStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

                if (contentLength.HasValue && contentLength.Value > 0)
                {
                    fileTask.Increment(bytesRead);
                }
            }

            if (!contentLength.HasValue || contentLength.Value <= 0)
            {
                fileTask.Value = 1;
            }
            else
            {
                fileTask.Value = fileTask.MaxValue;
            }

            entry.Status = "downloaded";
            entry.DownloadedAtUtc = DateTimeOffset.UtcNow;
            return DownloadStatus.Downloaded;
        }
        catch (Exception ex)
        {
            entry.Status = "failed";
            entry.Error = ex.Message;
            return DownloadStatus.Failed;
        }
    }

    private static BackgroundManifestEntry CreateManifestEntry(
        BackgroundDownloadOptions options,
        WallhavenWallpaper wallpaper,
        int index)
    {
        var fileExtension = ResolveFileExtension(wallpaper);
        var fileNameWithoutExtension = options.FileNamePattern
            .Replace("{index}", index.ToString("D5"), StringComparison.Ordinal)
            .Replace("{id}", wallpaper.Id, StringComparison.Ordinal)
            .Replace("{width}", wallpaper.DimensionX.ToString(), StringComparison.Ordinal)
            .Replace("{height}", wallpaper.DimensionY.ToString(), StringComparison.Ordinal);

        var fileName = $"{fileNameWithoutExtension}{fileExtension}";
        var localPath = Path.Combine(options.OutputDirectory, fileName);

        return new BackgroundManifestEntry
        {
            Id = wallpaper.Id,
            FileName = fileName,
            LocalPath = localPath,
            SourceUrl = wallpaper.Url,
            DownloadUrl = wallpaper.Path,
            Purity = wallpaper.Purity,
            Category = wallpaper.Category,
            Width = wallpaper.DimensionX,
            Height = wallpaper.DimensionY,
            Resolution = wallpaper.Resolution,
            FileSize = wallpaper.FileSize,
            FileType = wallpaper.FileType,
            CreatedAt = wallpaper.CreatedAt,
            Colors = wallpaper.Colors,
            Status = "planned"
        };
    }

    private static string ResolveFileExtension(WallhavenWallpaper wallpaper)
    {
        var extension = Path.GetExtension(wallpaper.Path);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }

        if (string.Equals(wallpaper.FileType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            return ".png";
        }

        return ".jpg";
    }

    private static int ResolvePageTaskTotal(BackgroundDownloadOptions options)
    {
        if (options.MaxPages > 0)
        {
            return options.MaxPages;
        }

        var approximatePages = (int)Math.Ceiling(options.Count / 24.0);
        return Math.Max(1, approximatePages);
    }

    private static int? ResolveLastPageToScan(BackgroundDownloadOptions options)
    {
        if (options.MaxPages <= 0)
        {
            return null;
        }

        return options.StartPage + options.MaxPages - 1;
    }

    private enum DownloadStatus
    {
        Downloaded,
        Skipped,
        Failed
    }
}
