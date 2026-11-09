using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Repositories;

/// <summary>The ads to pin on the map (the newest, when there are more than asked for), plus how many matched.</summary>
public record AdPinsPage(IReadOnlyList<Ad> Ads, int Total);
