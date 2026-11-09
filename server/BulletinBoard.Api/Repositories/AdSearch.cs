using BulletinBoard.Api.Geo;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

/// <summary>
/// What to find, as the repository understands it (no HTTP details). Every criterion is optional and
/// they combine with AND. Results always come newest first.
/// </summary>
public record AdSearch
{
    /// <summary>Text the title must contain (case-insensitive).</summary>
    public string? Title { get; init; }

    public Category? Category { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    /// <summary>The user's location: each match gets its distance from here.</summary>
    public GeoPoint? Location { get; init; }

    /// <summary>With <see cref="Location"/>: only ads within this many km.</summary>
    public double? RadiusKm { get; init; }

    /// <summary>Only ads inside this rectangle (the map area in view).</summary>
    public BoundingBox? Area { get; init; }
}
