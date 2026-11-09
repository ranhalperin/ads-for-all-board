using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Storage;

/// <summary>Where the ads file lives. Reads and writes of ads.json happen only while holding the lock.</summary>
public interface IFileStore
{
    /// <summary>Loads every ad from ads.json (streamed, no giant string in memory).</summary>
    Task<List<Ad>> ReadAdsAsync(CancellationToken ct);

    /// <summary>Saves every ad: writes a temp file, then renames it over ads.json, so a crash never leaves half a file.</summary>
    Task WriteAdsAsync(IReadOnlyCollection<Ad> ads, CancellationToken ct);

    /// <summary>
    /// The number in ads.version, or null if it can't be read right now (missing, or caught mid-write).
    /// Safe to call without the lock; every server calls it on every read.
    /// </summary>
    Task<long?> ReadVersionAsync(CancellationToken ct);

    Task WriteVersionAsync(long version, CancellationToken ct);
}
