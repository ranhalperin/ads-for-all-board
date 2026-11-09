namespace BulletinBoard.Api.Repositories;

/// <summary>One page of matches, newest first, plus how many matched in all.</summary>
public sealed record AdSearchPage(IReadOnlyList<AdMatch> Items, int Total);
