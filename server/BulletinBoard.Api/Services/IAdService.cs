using BulletinBoard.Api.Dtos;

namespace BulletinBoard.Api.Services;

public interface IAdService
{
    Task<PagedResult<AdResponse>> SearchAsync(AdQuery query, CancellationToken ct);
    Task<PinsResult> GetPinsAsync(AdQuery query, CancellationToken ct);
    Task<AdResponse> GetAsync(Guid id, CancellationToken ct);
    Task<AdResponse> CreateAsync(AdUpsertRequest request, CancellationToken ct);
    Task<AdResponse> UpdateAsync(Guid id, AdUpsertRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
