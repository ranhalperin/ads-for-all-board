using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BulletinBoard.Api.Controllers;

/// <summary>CRUD for ads. There are no accounts: an ad's contact name is how its author finds it again.</summary>
[ApiController]
[Route("api/ads")]
[Produces("application/json")]
public class AdsController(IAdService service) : ControllerBase
{
    /// <summary>Search ads: filters, newest first, one page.</summary>
    [HttpGet]
    public Task<PagedResult<AdResponse>> Search([FromQuery] AdQuery query, CancellationToken ct) =>
        service.SearchAsync(query, ct);

    /// <summary>Light pins for the map, with the same filters (paging is ignored).</summary>
    [HttpGet("pins")]
    public Task<PinsResult> Pins([FromQuery] AdQuery query, CancellationToken ct) =>
        service.GetPinsAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<AdResponse> Get(Guid id, CancellationToken ct) =>
        service.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<AdResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AdResponse>> Create(AdUpsertRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public Task<AdResponse> Update(Guid id, AdUpsertRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
