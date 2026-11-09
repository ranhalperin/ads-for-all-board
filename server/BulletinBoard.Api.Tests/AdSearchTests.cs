using BulletinBoard.Api.Geo;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Repositories;

namespace BulletinBoard.Api.Tests;

/// <summary>The search, run by the repository: filters, distance, newest first, paging and pins.</summary>
public class AdSearchTests : IDisposable
{
    // Tel Aviv centre; Ramat Gan ~4 km away; Jerusalem ~54 km away.
    private static readonly GeoPoint Tlv = new(32.0853, 34.7818);

    private readonly TempStore _store = new();
    private readonly JsonFileAdRepository _repository;

    public AdSearchTests() => _repository = _store.NewServer();

    public void Dispose() => _store.Dispose();

    private async Task SeedAsync()
    {
        var t = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        await _repository.AddAsync(Ads.Make("City bike", Category.ForSale, 450, Tlv.Lat, Tlv.Lng, created: t.AddHours(1)), default);
        await _repository.AddAsync(Ads.Make("Mountain BIKE", Category.ForSale, 1200, 32.0809, 34.8143, created: t.AddHours(2)), default);
        await _repository.AddAsync(Ads.Make("Sofa", Category.ForSale, null, 31.7683, 35.2137, created: t.AddHours(3)), default);
        await _repository.AddAsync(Ads.Make("Waiter wanted", Category.Jobs, null, 32.0636, 34.7722, created: t.AddHours(4)), default);
    }

    private async Task<IEnumerable<string>> Titles(AdSearch search) =>
        (await _repository.SearchAsync(search, 1, 20, default)).Items.Select(m => m.Ad.Title);

    [Fact]
    public async Task Title_matches_part_of_the_title_ignoring_case()
    {
        await SeedAsync();
        Assert.Equal(["Mountain BIKE", "City bike"], await Titles(new AdSearch { Title = "bik" }));
    }

    [Fact]
    public async Task Filters_by_category_and_price_range()
    {
        await SeedAsync();
        Assert.Equal(["City bike"], await Titles(new AdSearch { Category = Category.ForSale, MinPrice = 400, MaxPrice = 1000 }));
    }

    [Fact]
    public async Task Distance_keeps_ads_inside_the_radius_and_gives_each_its_distance()
    {
        await SeedAsync();
        var found = await _repository.SearchAsync(new AdSearch { Location = Tlv, RadiusKm = 10 }, 1, 20, default);

        Assert.Equal(["Waiter wanted", "Mountain BIKE", "City bike"], found.Items.Select(m => m.Ad.Title));
        Assert.Equal(0, found.Items.Single(m => m.Ad.Title == "City bike").DistanceKm);
        Assert.All(found.Items, m => Assert.InRange(m.DistanceKm!.Value, 0, 10));
    }

    [Fact]
    public async Task Results_are_always_newest_first()
    {
        await SeedAsync();
        Assert.Equal(["Waiter wanted", "Sofa", "Mountain BIKE", "City bike"], await Titles(new AdSearch()));
    }

    [Fact]
    public async Task Map_area_keeps_only_visible_ads()
    {
        await SeedAsync();
        Assert.Equal(["Sofa"], await Titles(new AdSearch { Area = new BoundingBox(31.5, 35.0, 32.0, 35.5) }));
    }

    [Fact]
    public async Task Pages_return_the_total_and_the_requested_slice()
    {
        await SeedAsync();
        var page2 = await _repository.SearchAsync(new AdSearch(), 2, 3, default);
        Assert.Equal(4, page2.Total);
        Assert.Equal("City bike", Assert.Single(page2.Items).Ad.Title); // oldest, on page 2 of newest-first
    }

    [Fact]
    public async Task Pins_keep_the_newest_when_more_match_than_asked()
    {
        await SeedAsync();
        var pins = await _repository.GetPinsAsync(new AdSearch(), 2, default);
        Assert.Equal(4, pins.Total);
        Assert.Equal(["Waiter wanted", "Sofa"], pins.Ads.Select(a => a.Title));
    }
}
