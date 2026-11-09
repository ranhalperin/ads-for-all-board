using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

public interface IAdRepository
{
    /// <summary>Every ad, as an immutable snapshot that is current as of this call.</summary>
    Task<IReadOnlyList<Ad>> GetAllAsync(CancellationToken ct);

    Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct);

    Task AddAsync(Ad ad, CancellationToken ct);

    /// <summary>Saves <paramref name="ad"/> in place of the stored ad with the same id. Throws NotFound if it no longer exists.</summary>
    Task UpdateAsync(Ad ad, CancellationToken ct);

    /// <summary>Removes an ad. Throws NotFound if missing.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct);
}
