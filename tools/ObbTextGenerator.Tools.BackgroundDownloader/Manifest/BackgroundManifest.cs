namespace ObbTextGenerator;

public sealed class BackgroundManifest
{
    public string Tool { get; init; } = "ObbTextGenerator.Tools.BackgroundDownloader";

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public List<BackgroundManifestEntry> Entries { get; init; } = [];

    public void AddOrUpdate(BackgroundManifestEntry entry)
    {
        for (var index = 0; index < Entries.Count; index++)
        {
            var existingEntry = Entries[index];
            if (string.Equals(existingEntry.Id, entry.Id, StringComparison.OrdinalIgnoreCase))
            {
                Entries[index] = entry;
                UpdatedAtUtc = DateTimeOffset.UtcNow;
                return;
            }
        }

        Entries.Add(entry);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
