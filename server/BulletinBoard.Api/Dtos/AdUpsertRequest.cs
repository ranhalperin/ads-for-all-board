using System.ComponentModel.DataAnnotations;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Dtos;

/// <summary>Body of POST and PUT. Id, owner and dates are set by the server, never by the client.</summary>
public sealed class AdUpsertRequest : IValidatableObject
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Title { get; init; } = "";

    [Required, StringLength(2000, MinimumLength = 1)]
    public string Description { get; init; } = "";

    [Required]
    public Category? Category { get; init; }

    [Range(typeof(decimal), "0", "100000000")]
    public decimal? Price { get; init; }

    [Required, StringLength(60, MinimumLength = 2)]
    public string ContactName { get; init; } = "";

    [Required, RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    public string ContactPhone { get; init; } = "";

    [StringLength(500)]
    public string? ImageUrl { get; init; }

    [Required, Range(-90, 90)]
    public double? Latitude { get; init; }

    [Required, Range(-180, 180)]
    public double? Longitude { get; init; }

    [StringLength(300)]
    public string? Address { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(ImageUrl)
            && !(Uri.TryCreate(ImageUrl, UriKind.Absolute, out var uri)
                 && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
        {
            yield return new ValidationResult("Image URL must be an http(s) link.", [nameof(ImageUrl)]);
        }
    }
}
