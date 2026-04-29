using System.Text.Json.Serialization;

namespace ObbTextGenerator;

public sealed class WallhavenSearchResponse
{
    [JsonPropertyName("data")]
    public List<WallhavenWallpaper> Data { get; init; } = [];

    [JsonPropertyName("meta")]
    public WallhavenSearchMeta Meta { get; init; } = new();
}
