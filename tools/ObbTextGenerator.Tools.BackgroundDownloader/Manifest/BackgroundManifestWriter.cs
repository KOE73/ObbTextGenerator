using System.Text.Json;

namespace ObbTextGenerator;

public sealed class BackgroundManifestWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public BackgroundManifest LoadOrCreate(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return new BackgroundManifest();
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<BackgroundManifest>(manifestJson, SerializerOptions);
        return manifest ?? new BackgroundManifest();
    }

    public void Save(string manifestPath, BackgroundManifest manifest)
    {
        var manifestDirectory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrWhiteSpace(manifestDirectory))
        {
            Directory.CreateDirectory(manifestDirectory);
        }

        manifest.UpdatedAtUtc = DateTimeOffset.UtcNow;
        var manifestJson = JsonSerializer.Serialize(manifest, SerializerOptions);
        File.WriteAllText(manifestPath, manifestJson);
    }
}
