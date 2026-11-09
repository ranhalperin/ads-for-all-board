using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Geo;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Repositories;
using BulletinBoard.Api.Storage;

namespace BulletinBoard.Api.Services;

/// <summary>Business rules: turns API requests into repository calls, and sets the fields the server owns (id, dates).</summary>
public sealed class AdService(IAdRepository repository, StorageOptions options, TimeProvider clock) : IAdService
{
    public async Task<PagedResult<AdResponse>> SearchAsync(AdQuery query, CancellationToken ct)
    {
        var found = await repository.SearchAsync(ToSearch(query), query.Page, query.PageSize, ct);
        var items = found.Items.Select(m => AdResponse.From(m.Ad, m.DistanceKm)).ToList();
        return new PagedResult<AdResponse>(items, found.Total, query.Page, query.PageSize);
    }

    public async Task<PinsResult> GetPinsAsync(AdQuery query, CancellationToken ct)
    {
        var found = await repository.GetPinsAsync(ToSearch(query), options.MaxPins, ct);
        var pins = found.Ads
            .Select(a => new AdPin(a.Id, a.Title, a.Category, a.Latitude, a.Longitude))
            .ToList();
        return new PinsResult(pins, found.Total, Truncated: found.Total > pins.Count);
    }

    public async Task<AdResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var ad = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ad not found.");
        return AdResponse.From(ad);
    }

    public async Task<AdResponse> CreateAsync(AdUpsertRequest request, CancellationToken ct)
    {
        var ad = Apply(new Ad
        {
            Id = Guid.NewGuid(),
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            Title = "", Description = "", Category = Category.Other, ContactName = "", ContactPhone = "",
            Latitude = 0, Longitude = 0,
        }, request);
        await repository.AddAsync(ad, ct);
        return AdResponse.From(ad);
    }

    public async Task<AdResponse> UpdateAsync(Guid id, AdUpsertRequest request, CancellationToken ct)
    {
        var existing = await repository.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ad not found.");
        var updated = Apply(existing, request) with { UpdatedAt = clock.GetUtcNow().UtcDateTime };
        await repository.UpdateAsync(updated, ct);
        return AdResponse.From(updated);
    }

    public Task DeleteAsync(Guid id, CancellationToken ct) => repository.DeleteAsync(id, ct);

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

    /// <summary>The API query string as a repository search. Blank text means "no title filter".</summary>
    private static AdSearch ToSearch(AdQuery q) => new()
    {
        Title = NullIfBlank(q.Search),
        Category = q.Category,
        MinPrice = q.MinPrice,
        MaxPrice = q.MaxPrice,
        Location = q.HasCentre ? new GeoPoint(q.Lat!.Value, q.Lng!.Value) : null,
        RadiusKm = q.RadiusKm,
        Area = q.HasBounds ? new BoundingBox(q.South!.Value, q.West!.Value, q.North!.Value, q.East!.Value) : null,
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
