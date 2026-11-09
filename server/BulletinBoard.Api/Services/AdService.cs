using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Repositories;
using BulletinBoard.Api.Storage;

namespace BulletinBoard.Api.Services;

/// <summary>Business rules: searching, ownership, and the fields the server sets itself.</summary>
public sealed class AdService(IAdRepository repository, StorageOptions options, TimeProvider clock) : IAdService
{
    public async Task<PagedResult<AdResponse>> SearchAsync(AdQuery query, string? ownerId, CancellationToken ct)
    {
        var matches = AdFilter.Apply(await repository.GetAllAsync(ct), query, ownerId);
        var page = AdFilter.Sort(matches, query.Sort)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => AdResponse.From(m.Ad, ownerId, m.DistanceKm))
            .ToList();
        return new PagedResult<AdResponse>(page, matches.Count, query.Page, query.PageSize);
    }

    public async Task<PinsResult> GetPinsAsync(AdQuery query, string? ownerId, CancellationToken ct)
    {
        var matches = AdFilter.Apply(await repository.GetAllAsync(ct), query, ownerId);
        var truncated = matches.Count > options.MaxPins;
        // Sorting costs time, so only when we must choose which pins to leave out (newest win).
        IEnumerable<AdFilter.Match> chosen = truncated
            ? AdFilter.Sort(matches, AdSort.Newest).Take(options.MaxPins)
            : matches;
        var pins = chosen
            .Select(m => new AdPin(m.Ad.Id, m.Ad.Title, m.Ad.Category, m.Ad.Latitude, m.Ad.Longitude))
            .ToList();
        return new PinsResult(pins, matches.Count, truncated);
    }

    public async Task<AdResponse> GetAsync(Guid id, string? ownerId, CancellationToken ct)
    {
        var ad = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ad not found.");
        return AdResponse.From(ad, ownerId);
    }

    public async Task<AdResponse> CreateAsync(AdUpsertRequest request, string ownerId, CancellationToken ct)
    {
        var ad = Apply(new Ad
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            Title = "", Description = "", Category = Category.Other, ContactName = "", ContactPhone = "",
            Latitude = 0, Longitude = 0,
        }, request);
        await repository.AddAsync(ad, ct);
        return AdResponse.From(ad, ownerId);
    }

    public async Task<AdResponse> UpdateAsync(Guid id, AdUpsertRequest request, string ownerId, CancellationToken ct)
    {
        var updated = await repository.UpdateAsync(id, existing =>
        {
            EnsureOwner(existing, ownerId, "edit");
            return Apply(existing, request) with { UpdatedAt = clock.GetUtcNow().UtcDateTime };
        }, ct);
        return AdResponse.From(updated, ownerId);
    }

    public Task DeleteAsync(Guid id, string ownerId, CancellationToken ct) =>
        repository.DeleteAsync(id, existing => EnsureOwner(existing, ownerId, "delete"), ct);

    private static void EnsureOwner(Ad ad, string ownerId, string action)
    {
        if (ad.OwnerId != ownerId)
            throw new ForbiddenException($"You can only {action} your own ads.");
    }

    /// <summary>Copies the user's fields onto an ad, trimmed; empty optional text becomes null.</summary>
    private static Ad Apply(Ad ad, AdUpsertRequest r) => ad with
    {
        Title = r.Title.Trim(),
        Description = r.Description.Trim(),
        Category = r.Category ?? Category.Other,
        Price = r.Price,
        ContactName = r.ContactName.Trim(),
        ContactPhone = r.ContactPhone.Trim(),
        ImageUrl = NullIfBlank(r.ImageUrl),
        Latitude = r.Latitude ?? 0,
        Longitude = r.Longitude ?? 0,
        Address = NullIfBlank(r.Address),
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
