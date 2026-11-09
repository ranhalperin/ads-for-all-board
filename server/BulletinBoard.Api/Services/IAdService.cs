using BulletinBoard.Api.Dtos;

namespace BulletinBoard.Api.Services;

public interface IAdService
{
    Task<PagedResult<AdResponse>> SearchAsync(AdQuery query, string? ownerId, CancellationToken ct);
    Task<PinsResult> GetPinsAsync(AdQuery query, string? ownerId, CancellationToken ct);
    Task<AdResponse> GetAsync(Guid id, string? ownerId, CancellationToken ct);
    Task<AdResponse> CreateAsync(AdUpsertRequest request, string ownerId, CancellationToken ct);
    Task<AdResponse> UpdateAsync(Guid id, AdUpsertRequest request, string ownerId, CancellationToken ct);
    Task DeleteAsync(Guid id, string ownerId, CancellationToken ct);
}
