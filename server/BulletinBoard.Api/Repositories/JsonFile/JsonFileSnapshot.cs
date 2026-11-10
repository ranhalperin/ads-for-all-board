using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories.JsonFile;

/// <summary>
/// The ads of <see cref="JsonFileAdRepository"/> as they are in ads.json.
/// Immutable: a change builds a new snapshot and the repository swaps it in one step,
/// so readers never see a half-applied change.
/// </summary>
internal class JsonFileSnapshot
{
    public JsonFileSnapshot(List<Ad> ads)
    {
        Ads = ads.AsReadOnly();
        ById = ads.ToDictionary(a => a.Id);
    }

    /// <summary>Every ad, in file order.</summary>
    public IReadOnlyList<Ad> Ads { get; }

    /// <summary>The same ads by id, for fast single-ad lookups.</summary>
    public IReadOnlyDictionary<Guid, Ad> ById { get; }
}
