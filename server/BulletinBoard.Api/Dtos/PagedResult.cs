namespace BulletinBoard.Api.Dtos;

/// <summary>One page of results plus the total number of matches.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
