using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Dtos;

/// <summary>
/// An ad as sent to the browser. The owner ID is never sent: the server says only whether
/// the ad is the caller's (<see cref="IsMine"/>), so nobody can copy someone else's owner ID.
/// </summary>
public sealed record AdResponse(
    Guid Id,
    string Title,
    string Description,
    Category Category,
    decimal? Price,
    string ContactName,
    string ContactPhone,
    string? ImageUrl,
    double Latitude,
    double Longitude,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsMine,
    double? DistanceKm)
{
    public static AdResponse From(Ad ad, string? callerOwnerId, double? distanceKm = null) => new(
        ad.Id, ad.Title, ad.Description, ad.Category, ad.Price, ad.ContactName, ad.ContactPhone,
        ad.ImageUrl, ad.Latitude, ad.Longitude, ad.Address, ad.CreatedAt, ad.UpdatedAt,
        callerOwnerId is not null && ad.OwnerId == callerOwnerId,
        distanceKm is null ? null : Math.Round(distanceKm.Value, 2));
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>The light shape the map needs for one pin.</summary>
public sealed record AdPin(Guid Id, string Title, Category Category, double Latitude, double Longitude);

/// <summary>Pins for the map. <see cref="Truncated"/> is true when more ads matched than were sent.</summary>
public sealed record PinsResult(IReadOnlyList<AdPin> Pins, int Total, bool Truncated);
