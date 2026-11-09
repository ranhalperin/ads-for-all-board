using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Services;

namespace BulletinBoard.Api.Tests;

public class AdServiceTests : IDisposable
{
    private const string Me = "owner-me-000001";
    private const string Other = "owner-other-0001";

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
        await repo.AddAsync(Ads.Make("City bike", Category.ForSale, 450, TlvLat, TlvLng, Me, t.AddHours(1)), default);
        await repo.AddAsync(Ads.Make("Mountain BIKE", Category.ForSale, 1200, 32.0809, 34.8143, Other, t.AddHours(2)), default);
        await repo.AddAsync(Ads.Make("Sofa", Category.ForSale, null, 31.7683, 35.2137, Other, t.AddHours(3)), default);
        await repo.AddAsync(Ads.Make("Waiter wanted", Category.Jobs, null, 32.0636, 34.7722, Me, t.AddHours(4)), default);
    }

    private Task<PagedResult<AdResponse>> Search(AdQuery q, string? owner = Me) => _service.SearchAsync(q, owner, default);

    [Fact]
    public async Task Search_matches_the_title_only_ignoring_case()
    {
        await SeedAsync();
        var result = await Search(new AdQuery { Search = "bik" });
        Assert.Equal(["Mountain BIKE", "City bike"], result.Items.Select(a => a.Title));
    }

    [Fact]
    public async Task Filters_by_category_and_price_range()
    {
        await SeedAsync();
        var result = await Search(new AdQuery { Category = Category.ForSale, MinPrice = 400, MaxPrice = 1000 });
        Assert.Equal("City bike", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task Distance_filter_keeps_ads_inside_the_radius_newest_first_with_distance()
    {
        await SeedAsync();
        var result = await Search(new AdQuery { Lat = TlvLat, Lng = TlvLng, RadiusKm = 10 });

        Assert.Equal(["Waiter wanted", "Mountain BIKE", "City bike"], result.Items.Select(a => a.Title));
        Assert.Equal(0, result.Items.Single(a => a.Title == "City bike").DistanceKm);
        Assert.All(result.Items, a => Assert.InRange(a.DistanceKm!.Value, 0, 10));
    }

    [Fact]
    public async Task Results_are_always_newest_first()
    {
        await SeedAsync();
        var result = await Search(new AdQuery());
        Assert.Equal(["Waiter wanted", "Sofa", "Mountain BIKE", "City bike"], result.Items.Select(a => a.Title));
    }

    [Fact]
    public async Task Map_area_filter_keeps_only_visible_ads()
    {
        await SeedAsync();
        var result = await Search(new AdQuery { North = 32.0, South = 31.5, East = 35.5, West = 35.0 });
        Assert.Equal("Sofa", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task Mine_only_and_is_mine_follow_the_caller()
    {
        await SeedAsync();
        var mine = await Search(new AdQuery { MineOnly = true });
        Assert.Equal(["Waiter wanted", "City bike"], mine.Items.Select(a => a.Title));
        Assert.All(mine.Items, a => Assert.True(a.IsMine));

        var asStranger = await Search(new AdQuery(), owner: null);
        Assert.All(asStranger.Items, a => Assert.False(a.IsMine));
    }

    [Fact]
    public async Task Pages_return_the_total_and_the_requested_slice()
    {
        await SeedAsync();
        var page2 = await Search(new AdQuery { Page = 2, PageSize = 3 });
        Assert.Equal(4, page2.Total);
        Assert.Equal("City bike", Assert.Single(page2.Items).Title); // oldest, on page 2 of newest-first
    }

    [Fact]
    public async Task Pins_are_capped_and_flag_truncation()
    {
        await SeedAsync();
        _store.Options.MaxPins = 2;
        var pins = await _service.GetPinsAsync(new AdQuery(), Me, default);
        Assert.Equal(4, pins.Total);
        Assert.True(pins.Truncated);
        Assert.Equal(["Waiter wanted", "Sofa"], pins.Pins.Select(p => p.Title)); // newest kept
    }

    [Fact]
    public async Task Create_sets_owner_id_and_dates_and_trims_text()
    {
        var created = await _service.CreateAsync(Request(title: "  Lamp  ", image: " "), Me, default);
        Assert.Equal("Lamp", created.Title);
        Assert.Null(created.ImageUrl);
        Assert.True(created.IsMine);
        Assert.Null(created.UpdatedAt);
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task Only_the_owner_can_edit_or_delete()
    {
        var ad = await _service.CreateAsync(Request(), Me, default);

        await Assert.ThrowsAsync<ForbiddenException>(() => _service.UpdateAsync(ad.Id, Request(title: "Hacked"), Other, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.DeleteAsync(ad.Id, Other, default));

        var edited = await _service.UpdateAsync(ad.Id, Request(title: "Edited"), Me, default);
        Assert.Equal("Edited", edited.Title);
        Assert.NotNull(edited.UpdatedAt);
        Assert.Equal(ad.CreatedAt, edited.CreatedAt);

        await _service.DeleteAsync(ad.Id, Me, default);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(ad.Id, Me, default));
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
