using System.Text.RegularExpressions;
using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BulletinBoard.Api.Controllers;

/// <summary>
/// CRUD for ads. The caller is identified by the X-Owner-Id header, an ID the browser creates once and keeps.
/// It's required to create, edit or delete; optional for reading (it only marks which ads are the caller's).
/// </summary>
[ApiController]
[Route("api/ads")]
[Produces("application/json")]
public sealed partial class AdsController(IAdService service) : ControllerBase
{
    public const string OwnerHeader = "X-Owner-Id";

    /// <summary>Search ads: filters, newest first, one page.</summary>
    [HttpGet]
    public Task<PagedResult<AdResponse>> Search([FromQuery] AdQuery query, CancellationToken ct) =>
        service.SearchAsync(query, OwnerId(required: query.MineOnly), ct);

    /// <summary>Light pins for the map, with the same filters (paging is ignored).</summary>
    [HttpGet("pins")]
    public Task<PinsResult> Pins([FromQuery] AdQuery query, CancellationToken ct) =>
        service.GetPinsAsync(query, OwnerId(required: query.MineOnly), ct);

    [HttpGet("{id:guid}")]
    public Task<AdResponse> Get(Guid id, CancellationToken ct) =>
        service.GetAsync(id, OwnerId(required: false), ct);

    [HttpPost]
    [ProducesResponseType<AdResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AdResponse>> Create(AdUpsertRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, OwnerId(required: true)!, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public Task<AdResponse> Update(Guid id, AdUpsertRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, OwnerId(required: true)!, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, OwnerId(required: true)!, ct);
        return NoContent();
    }

    private string? OwnerId(bool required)
    {
        var value = Request.Headers[OwnerHeader].ToString();
        if (string.IsNullOrEmpty(value))
            return required ? throw new BadRequestException($"The {OwnerHeader} header is required.") : null;
        if (!OwnerIdFormat().IsMatch(value))
            throw new BadRequestException($"The {OwnerHeader} header is not a valid ID.");
        return value;
    }

    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex OwnerIdFormat();
}
