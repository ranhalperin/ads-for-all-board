using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Dtos;

/// <summary>
/// An ad as sent to the browser, with its distance when the search had a location.
/// </summary>
public record AdResponse(
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
    double? DistanceKm)
{
    public static AdResponse From(Ad ad, double? distanceKm = null) => new(
        ad.Id, ad.Title, ad.Description, ad.Category, ad.Price, ad.ContactName, ad.ContactPhone,
        ad.ImageUrl, ad.Latitude, ad.Longitude, ad.Address, ad.CreatedAt, ad.UpdatedAt,
        distanceKm is null ? null : Math.Round(distanceKm.Value, 2));
}
