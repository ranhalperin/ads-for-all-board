using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

/// <summary>
/// The ads of <see cref="JsonFileAdRepository"/> as they were in ads.json at one version.
/// Immutable: a change builds a new snapshot and the repository swaps it in one step,
/// so readers never see a half-applied change.
/// </summary>
internal sealed class Snapshot
{
    /// <summary>Before the first load. Its version (-1) never matches the file's.</summary>
    public static readonly Snapshot NotLoaded = new(-1, []);

    public Snapshot(long version, List<Ad> ads)
    {
        Version = version;
        Ads = ads.AsReadOnly();
        ById = ads.ToDictionary(a => a.Id);
    }

    /// <summary>The ads.version value these ads were read or written at.</summary>
    public long Version { get; }

    /// <summary>Every ad, in file order.</summary>
    public IReadOnlyList<Ad> Ads { get; }

    /// <summary>The same ads by id, for fast single-ad lookups.</summary>
    public IReadOnlyDictionary<Guid, Ad> ById { get; }

    public bool IsLoaded => Version >= 0;
}
