using System.Text.Json.Serialization;

namespace ObbTextGenerator;

public sealed class WallhavenWallpaper
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("purity")]
    public string Purity { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("dimension_x")]
    public int DimensionX { get; init; }

    [JsonPropertyName("dimension_y")]
    public int DimensionY { get; init; }

    [JsonPropertyName("resolution")]
    public string Resolution { get; init; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; init; }

    [JsonPropertyName("file_type")]
    public string FileType { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("colors")]
    public List<string> Colors { get; init; } = [];
}
