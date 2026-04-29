using System.Net;
using System.Text.Json;

namespace ObbTextGenerator;

public sealed class WallhavenApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly BackgroundDownloadOptions options;

    public WallhavenApiClient(HttpClient httpClient, BackgroundDownloadOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<WallhavenSearchResponse> SearchAsync(int page, CancellationToken cancellationToken)
    {
        var requestUri = BuildSearchUri(page);
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new InvalidOperationException("Wallhaven API rate limit reached. Increase --api-delay-ms.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Wallhaven API returned 401 Unauthorized. Check --api-key or WALLHAVEN_API_KEY.");
        }

        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var searchResponse = await JsonSerializer.DeserializeAsync<WallhavenSearchResponse>(
            responseStream,
            SerializerOptions,
            cancellationToken);

        return searchResponse ?? new WallhavenSearchResponse();
    }

    private Uri BuildSearchUri(int page)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("page", page.ToString()),
            new("categories", options.Categories),
            new("purity", options.Purity),
            new("sorting", options.Sorting),
            new("order", options.Order)
        };

        AddOptional(parameters, "q", options.Query);
        AddOptional(parameters, "topRange", options.TopRange);
        AddOptional(parameters, "atleast", options.AtLeast);
        AddOptional(parameters, "resolutions", options.Resolutions);
        AddOptional(parameters, "ratios", options.Ratios);
        AddOptional(parameters, "colors", options.Colors);
        AddOptional(parameters, "seed", options.Seed);
        AddOptional(parameters, "apikey", options.ApiKey);

        var query = string.Join("&", parameters.Select(parameter =>
        {
            var key = Uri.EscapeDataString(parameter.Key);
            var value = Uri.EscapeDataString(parameter.Value);
            return $"{key}={value}";
        }));

        return new Uri($"https://wallhaven.cc/api/v1/search?{query}");
    }

    private static void AddOptional(List<KeyValuePair<string, string>> parameters, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parameters.Add(new KeyValuePair<string, string>(key, value));
    }
}
