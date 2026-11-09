using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Services;

namespace BulletinBoard.Api.Tests;

public class AdServiceTests : IDisposable
{
    private const string Dana = "Dana";
    private const string Yossi = "Yossi";

    // Tel Aviv centre; Ramat Gan ~4 km away; Jerusalem ~54 km away.
    private const double TlvLat = 32.0853, TlvLng = 34.7818;

    private readonly TempStore _store = new();
    private readonly AdService _service;

    public AdServiceTests() => _service = new AdService(_store.NewServer(), _store.Options, TimeProvider.System);

    public void Dispose() => _store.Dispose();

    private async Task SeedAsync()
    {
        var repo = _store.NewServer();
        var t = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.AddAsync(Ads.Make("City bike", Category.ForSale, 450, TlvLat, TlvLng, Dana, t.AddHours(1)), default);
        await repo.AddAsync(Ads.Make("Mountain BIKE", Category.ForSale, 1200, 32.0809, 34.8143, Yossi, t.AddHours(2)), default);
        await repo.AddAsync(Ads.Make("Sofa", Category.ForSale, null, 31.7683, 35.2137, Yossi, t.AddHours(3)), default);
        await repo.AddAsync(Ads.Make("Waiter wanted", Category.Jobs, null, 32.0636, 34.7722, Dana, t.AddHours(4)), default);
    }

    private Task<PagedResult<AdResponse>> Search(AdQuery q) => _service.SearchAsync(q, default);

    [Fact]
    public async Task Query_reaches_the_repository_and_distances_come_back_rounded()
    {
        await SeedAsync();
        var result = await Search(new AdQuery { Lat = TlvLat, Lng = TlvLng, RadiusKm = 10 });

        Assert.Equal(["Waiter wanted", "Mountain BIKE", "City bike"], result.Items.Select(a => a.Title));
        Assert.Equal(0, result.Items.Single(a => a.Title == "City bike").DistanceKm);
        Assert.All(result.Items, a => Assert.InRange(a.DistanceKm!.Value, 0, 10));
    }

    [Fact]
    public async Task Pins_are_capped_and_flag_truncation()
    {
        await SeedAsync();
        _store.Options.MaxPins = 2;
        var pins = await _service.GetPinsAsync(new AdQuery(), default);
        Assert.Equal(4, pins.Total);
        Assert.True(pins.Truncated);
        Assert.Equal(["Waiter wanted", "Sofa"], pins.Pins.Select(p => p.Title)); // newest kept
    }

    [Fact]
    public async Task Create_sets_id_and_dates_and_trims_text()
    {
        var created = await _service.CreateAsync(Request(title: "  Lamp  ", image: " "), default);
        Assert.Equal("Lamp", created.Title);
        Assert.Null(created.ImageUrl);
        Assert.Null(created.UpdatedAt);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task Edit_keeps_the_created_date_and_delete_removes_the_ad()
    {
        var ad = await _service.CreateAsync(Request(), default);

        var edited = await _service.UpdateAsync(ad.Id, Request(title: "Edited"), default);
        Assert.Equal("Edited", edited.Title);
        Assert.NotNull(edited.UpdatedAt);
        Assert.Equal(ad.CreatedAt, edited.CreatedAt);

        await _service.DeleteAsync(ad.Id, default);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(ad.Id, default));
    }

    private static AdUpsertRequest Request(string title = "Lamp", string? image = null) => new()
    {
        Title = title,
        Description = "Desk lamp",
        Category = Category.ForSale,
        Price = 50,
        ContactName = "Dana",
        ContactPhone = "050-1234567",
        ImageUrl = image,
        Latitude = TlvLat,
        Longitude = TlvLng,
    };
}
