using BulletinBoard.Api.Geo;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

/// <summary>
/// Runs an <see cref="AdSearch"/> over ads held in memory. Used by <see cref="JsonFileAdRepository"/>;
/// a database repository would turn the same <see cref="AdSearch"/> into a query instead.
/// </summary>
internal static class AdFilter
{
    public static List<AdMatch> Apply(IReadOnlyList<Ad> ads, AdSearch search)
    {
        IEnumerable<Ad> result = ads;

        if (!string.IsNullOrEmpty(search.Title))
            result = result.Where(a => a.Title.Contains(search.Title, StringComparison.OrdinalIgnoreCase));

        if (search.Category is { } category)
            result = result.Where(a => a.Category == category);

        if (search.MinPrice is { } min)
            result = result.Where(a => a.Price >= min);

        if (search.MaxPrice is { } max)
            result = result.Where(a => a.Price <= max);

        if (search.Area is { } area)
            result = result.Where(a => area.Contains(a.Latitude, a.Longitude));

        if (search.Location is not { } here)
            return result.Select(a => new AdMatch(a, null)).ToList();

        if (search.RadiusKm is { } radius)
        {
            // Cheap rectangle check first; the exact distance only for what's inside it.
            var box = BoundingBox.Around(here.Lat, here.Lng, radius);
            result = result.Where(a => box.Contains(a.Latitude, a.Longitude));
        }

        var withDistance = result.Select(a => new AdMatch(a, GeoDistance.Km(here.Lat, here.Lng, a.Latitude, a.Longitude)));
        if (search.RadiusKm is { } r)
            withDistance = withDistance.Where(m => m.DistanceKm <= r);
        return withDistance.ToList();
    }

    /// <summary>Newest first; the id breaks ties so paging is stable.</summary>
    public static IEnumerable<AdMatch> NewestFirst(IEnumerable<AdMatch> matches) =>
        matches.OrderByDescending(m => m.Ad.CreatedAt).ThenBy(m => m.Ad.Id);
}
