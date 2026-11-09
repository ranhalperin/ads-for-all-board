using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

/// <summary>An ad that matched a search, with its distance when the search had a location.</summary>
public sealed record AdMatch(Ad Ad, double? DistanceKm);
