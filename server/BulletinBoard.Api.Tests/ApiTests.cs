using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BulletinBoard.Api.Dtos;
using BulletinBoard.Api.Storage;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BulletinBoard.Api.Tests;

/// <summary>End-to-end through HTTP: routing, validation, the owner header and error shapes.</summary>
public class ApiTests : IDisposable
{
    private const string Owner = "browser-owner-0001";
    private readonly TempStore _store = new();
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions Json = JsonDefaults.Configure(new JsonSerializerOptions());

    public ApiTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseSetting("Storage:FolderPath", _store.Folder.Path)
            .UseSetting("Storage:SeedFile", "no-seed.json"));
    }

    public void Dispose()
    {
        _factory.Dispose();
        _store.Dispose();
    }

    private HttpClient Client(string? owner = Owner)
    {
        var client = _factory.CreateClient();
        if (owner is not null) client.DefaultRequestHeaders.Add("X-Owner-Id", owner);
        return client;
    }

    private static object NewAd(string title = "Lamp") => new
    {
        title,
        description = "Desk lamp",
        category = "ForSale",
        price = 50,
        contactName = "Dana",
        contactPhone = "050-1234567",
        latitude = 32.0853,
        longitude = 34.7818,
    };

    [Fact]
    public async Task Full_crud_round_trip()
    {
        var client = Client();

        var create = await client.PostAsJsonAsync("/api/ads", NewAd());
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var ad = (await create.Content.ReadFromJsonAsync<AdResponse>(Json))!;
        Assert.Equal($"/api/ads/{ad.Id}", create.Headers.Location!.AbsolutePath, ignoreCase: true);
        Assert.True(ad.IsMine);

        var page = (await client.GetFromJsonAsync<PagedResult<AdResponse>>("/api/ads?search=lam", Json))!;
        Assert.Equal(1, page.Total);

        var update = await client.PutAsJsonAsync($"/api/ads/{ad.Id}", NewAd("Floor lamp"));
        update.EnsureSuccessStatusCode();
        Assert.Equal("Floor lamp", (await update.Content.ReadFromJsonAsync<AdResponse>(Json))!.Title);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/ads/{ad.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/ads/{ad.Id}")).StatusCode);
    }

    [Fact]
    public async Task Writing_without_the_owner_header_is_rejected()
    {
        var response = await Client(owner: null).PostAsJsonAsync("/api/ads", NewAd());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("X-Owner-Id", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Someone_elses_ad_cannot_be_edited_or_deleted()
    {
        var created = await Client().PostAsJsonAsync("/api/ads", NewAd());
        var ad = (await created.Content.ReadFromJsonAsync<AdResponse>(Json))!;
        var stranger = Client("browser-owner-0002");

        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.PutAsJsonAsync($"/api/ads/{ad.Id}", NewAd("Hacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.DeleteAsync($"/api/ads/{ad.Id}")).StatusCode);

        var seen = (await stranger.GetFromJsonAsync<AdResponse>($"/api/ads/{ad.Id}", Json))!;
        Assert.False(seen.IsMine);
        Assert.Equal("Lamp", seen.Title);
    }

    [Fact]
    public async Task Invalid_ad_returns_field_errors()
    {
        var response = await Client().PostAsJsonAsync("/api/ads", new { title = "x", category = "ForSale" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", body);
        Assert.Contains("Latitude", body);
        Assert.Contains("ContactPhone", body);
    }

    [Theory]
    [InlineData("/api/ads?radiusKm=5")]
    [InlineData("/api/ads?minPrice=10&maxPrice=5")]
    [InlineData("/api/ads?sort=distance")]
    [InlineData("/api/ads?north=32")]
    [InlineData("/api/ads?pageSize=500")]
    public async Task Invalid_query_returns_400(string url) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await Client().GetAsync(url)).StatusCode);

    [Fact]
    public async Task Owner_id_is_never_sent_to_the_browser()
    {
        await Client().PostAsJsonAsync("/api/ads", NewAd());
        var body = await Client("browser-owner-0002").GetStringAsync("/api/ads");
        Assert.DoesNotContain(Owner, body);
        Assert.DoesNotContain("ownerId", body);
    }

    [Fact]
    public async Task Pins_endpoint_returns_light_pins()
    {
        await Client().PostAsJsonAsync("/api/ads", NewAd());
        var pins = (await Client().GetFromJsonAsync<PinsResult>("/api/ads/pins?lat=32.08&lng=34.78&radiusKm=5", Json))!;
        Assert.Equal(1, pins.Total);
        Assert.False(pins.Truncated);
        Assert.Equal("Lamp", Assert.Single(pins.Pins).Title);
    }
}
