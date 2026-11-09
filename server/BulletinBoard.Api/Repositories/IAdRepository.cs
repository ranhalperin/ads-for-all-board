using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

public interface IAdRepository
{
    /// <summary>One page of the ads matching <paramref name="search"/>, newest first, plus the total.</summary>
    Task<AdSearchPage> SearchAsync(AdSearch search, int page, int pageSize, CancellationToken ct);

    /// <summary>Up to <paramref name="maxPins"/> matching ads for the map (the newest, when more match), plus the total.</summary>
    Task<AdPinsPage> GetPinsAsync(AdSearch search, int maxPins, CancellationToken ct);

    Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct);

    Task AddAsync(Ad ad, CancellationToken ct);

    /// <summary>Saves <paramref name="ad"/> in place of the stored ad with the same id. Throws NotFound if it no longer exists.</summary>
    Task UpdateAsync(Ad ad, CancellationToken ct);

    /// <summary>Removes an ad. Throws NotFound if missing.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct);
}
