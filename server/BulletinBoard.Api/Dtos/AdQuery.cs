using System.ComponentModel.DataAnnotations;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Dtos;

/// <summary>
/// Query string of GET /api/ads and GET /api/ads/pins. Every filter is optional; all combine with AND.
/// Results are always newest first.
/// </summary>
public class AdQuery : IValidatableObject
{
    /// <summary>Text to find in the ad's title (case-insensitive, partial).</summary>
    [StringLength(100)]
    public string? Search { get; init; }

    public Category? Category { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MinPrice { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MaxPrice { get; init; }

    /// <summary>Distance filter: the caller's location and a radius in km.</summary>
    [Range(-90, 90)] public double? Lat { get; init; }
    [Range(-180, 180)] public double? Lng { get; init; }
    [Range(0.1, 500)] public double? RadiusKm { get; init; }

    /// <summary>The visible map area. Used by the map's pins request, so only pins in view are sent.</summary>
    [Range(-90, 90)] public double? North { get; init; }
    [Range(-90, 90)] public double? South { get; init; }
    [Range(-180, 180)] public double? East { get; init; }
    [Range(-180, 180)] public double? West { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public bool HasCentre => Lat is not null && Lng is not null;
    public bool HasRadius => HasCentre && RadiusKm is not null;
    public bool HasBounds => North is not null && South is not null && East is not null && West is not null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice is { } min && MaxPrice is { } max && min > max)
            yield return new ValidationResult("Minimum price can't be above maximum price.", [nameof(MinPrice)]);

        if ((Lat is null) != (Lng is null))
            yield return new ValidationResult("Send both lat and lng.", [nameof(Lat)]);

        if (RadiusKm is not null && !HasCentre)
            yield return new ValidationResult("A radius needs lat and lng.", [nameof(RadiusKm)]);

        var boundsSent = new[] { North, South, East, West }.Count(v => v is not null);
        if (boundsSent is > 0 and < 4)
            yield return new ValidationResult("Send all four map bounds (north, south, east, west).", [nameof(North)]);
        else if (HasBounds && South > North)
            yield return new ValidationResult("South can't be above north.", [nameof(South)]);
    }
}
