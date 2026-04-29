namespace ObbTextGenerator;

public sealed class BackgroundManifestEntry
{
    public required string Id { get; init; }

    public required string FileName { get; init; }

    public required string LocalPath { get; init; }

    public required string SourceUrl { get; init; }

    public required string DownloadUrl { get; init; }

    public required string Purity { get; init; }

    public required string Category { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required string Resolution { get; init; }

    public required long FileSize { get; init; }

    public required string FileType { get; init; }

    public required string CreatedAt { get; init; }

    public required IReadOnlyList<string> Colors { get; init; }

    public required string Status { get; set; }

    public DateTimeOffset? DownloadedAtUtc { get; set; }

    public string? Error { get; set; }
}
