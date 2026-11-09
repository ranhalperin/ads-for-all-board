using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Repositories;
using BulletinBoard.Api.Storage;

namespace BulletinBoard.Api.Services;

/// <summary>Business rules: searching and the fields the server sets itself.</summary>
public sealed class AdService(IAdRepository repository, StorageOptions options, TimeProvider clock) : IAdService
{
    public async Task<PagedResult<AdResponse>> SearchAsync(AdQuery query, CancellationToken ct)
    {
        var matches = AdFilter.Apply(await repository.GetAllAsync(ct), query);
        var page = AdFilter.NewestFirst(matches)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(m => AdResponse.From(m.Ad, m.DistanceKm))
            .ToList();
        return new PagedResult<AdResponse>(page, matches.Count, query.Page, query.PageSize);
    }

    public async Task<PinsResult> GetPinsAsync(AdQuery query, CancellationToken ct)
    {
        var matches = AdFilter.Apply(await repository.GetAllAsync(ct), query);
        var truncated = matches.Count > options.MaxPins;
        // Sorting costs time, so only when we must choose which pins to leave out (newest win).
        IEnumerable<AdFilter.Match> chosen = truncated
            ? AdFilter.NewestFirst(matches).Take(options.MaxPins)
            : matches;
        var pins = chosen
            .Select(m => new AdPin(m.Ad.Id, m.Ad.Title, m.Ad.Category, m.Ad.Latitude, m.Ad.Longitude))
            .ToList();
        return new PinsResult(pins, matches.Count, truncated);
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

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
