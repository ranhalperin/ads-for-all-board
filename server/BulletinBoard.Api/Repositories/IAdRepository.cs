using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

public interface IAdRepository
{
    /// <summary>Every ad, as an immutable snapshot that is current as of this call.</summary>
    Task<IReadOnlyList<Ad>> GetAllAsync(CancellationToken ct);

    Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct);

    Task AddAsync(Ad ad, CancellationToken ct);

    /// <summary>
    /// Replaces an ad with what <paramref name="update"/> returns. <paramref name="update"/> runs inside the
    /// lock on fresh data, so a check it makes (such as ownership) can't be raced. Throws NotFound if missing.
    /// </summary>
    Task<Ad> UpdateAsync(Guid id, Func<Ad, Ad> update, CancellationToken ct);

    /// <summary>Removes an ad. <paramref name="check"/> runs inside the lock first and may throw to refuse.</summary>
    Task DeleteAsync(Guid id, Action<Ad> check, CancellationToken ct);
}
