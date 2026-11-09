namespace BulletinBoard.Api.Dtos;

/// <summary>Pins for the map. <see cref="Truncated"/> is true when more ads matched than were sent.</summary>
public sealed record PinsResult(IReadOnlyList<AdPin> Pins, int Total, bool Truncated);
