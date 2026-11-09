namespace BulletinBoard.Api.Models;

/// <summary>
/// An ad as stored in ads.json. Immutable: an edit creates a new instance (<c>with</c>),
/// so a list handed to readers never changes under them.
/// </summary>
public record Ad
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required Category Category { get; init; }
    public decimal? Price { get; init; }
    public required string ContactName { get; init; }
    public required string ContactPhone { get; init; }
    public string? ImageUrl { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public string? Address { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
