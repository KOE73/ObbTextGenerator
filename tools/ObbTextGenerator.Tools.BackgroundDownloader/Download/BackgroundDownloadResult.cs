namespace ObbTextGenerator;

public sealed class BackgroundDownloadResult
{
    public int DownloadedCount { get; set; }

    public int SkippedCount { get; set; }

    public int FailedCount { get; set; }

    public int ScannedPages { get; set; }
}
