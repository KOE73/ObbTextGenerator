using System.Text.Json.Serialization;

namespace ObbTextGenerator;

public sealed class WallhavenSearchMeta
{
    [JsonPropertyName("current_page")]
    public int CurrentPage { get; init; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; init; } = 1;

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("seed")]
    public string? Seed { get; init; }
}
