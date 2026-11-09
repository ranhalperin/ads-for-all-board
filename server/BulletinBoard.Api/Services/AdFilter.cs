using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Geo;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Services;

/// <summary>The search: every filter in <see cref="AdQuery"/> over the in-memory list, then newest first.</summary>
public static class AdFilter
{
    public readonly record struct Match(Ad Ad, double? DistanceKm);

    public static List<Match> Apply(IReadOnlyList<Ad> ads, AdQuery q)
    {
        IEnumerable<Ad> result = ads;

        var search = q.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            result = result.Where(a => a.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (q.Category is { } category)
            result = result.Where(a => a.Category == category);

        if (q.MinPrice is { } min)
            result = result.Where(a => a.Price >= min);

        if (q.MaxPrice is { } max)
            result = result.Where(a => a.Price <= max);

        var contactName = q.ContactName?.Trim();
        if (!string.IsNullOrEmpty(contactName))
            result = result.Where(a => string.Equals(a.ContactName, contactName, StringComparison.OrdinalIgnoreCase));

        if (q.HasBounds)
        {
            var visible = new BoundingBox(q.South!.Value, q.West!.Value, q.North!.Value, q.East!.Value);
            result = result.Where(a => visible.Contains(a.Latitude, a.Longitude));
        }

        if (!q.HasCentre)
            return result.Select(a => new Match(a, null)).ToList();

        var (lat, lng) = (q.Lat!.Value, q.Lng!.Value);
        if (q.RadiusKm is { } radius)
        {
            // Cheap rectangle check first; the exact distance only for what's inside it.
            var box = BoundingBox.Around(lat, lng, radius);
            result = result.Where(a => box.Contains(a.Latitude, a.Longitude));
        }

        var withDistance = result.Select(a => new Match(a, GeoDistance.Km(lat, lng, a.Latitude, a.Longitude)));
        if (q.RadiusKm is { } r)
            withDistance = withDistance.Where(m => m.DistanceKm <= r);
        return withDistance.ToList();
    }

    /// <summary>Newest first; the id breaks ties so paging is stable.</summary>
    public static IEnumerable<Match> NewestFirst(IEnumerable<Match> matches) =>
        matches.OrderByDescending(m => m.Ad.CreatedAt).ThenBy(m => m.Ad.Id);
}
