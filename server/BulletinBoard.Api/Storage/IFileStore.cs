using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Storage;

/// <summary>Where the ads file lives. The repository writes ads.json only while holding its lock.</summary>
public interface IFileStore
{
    /// <summary>Loads every ad from ads.json (streamed, no giant string in memory).</summary>
    Task<List<Ad>> ReadAdsAsync(CancellationToken ct);

    /// <summary>Saves every ad: writes a temp file, then renames it over ads.json, so a crash never leaves half a file.</summary>
    Task WriteAdsAsync(IReadOnlyCollection<Ad> ads, CancellationToken ct);
}
