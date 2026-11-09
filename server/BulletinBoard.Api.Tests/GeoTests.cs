using BulletinBoard.Api.Geo;

namespace BulletinBoard.Api.Tests;

public class GeoTests
{
    [Fact]
    public void Distance_TelAviv_to_Jerusalem_is_about_54_km()
    {
        var km = GeoDistance.Km(32.0853, 34.7818, 31.7683, 35.2137);
        Assert.InRange(km, 52, 56);
    }

    [Fact]
    public void Distance_to_same_point_is_zero() =>
        Assert.Equal(0, GeoDistance.Km(32, 34.8, 32, 34.8), precision: 6);

    [Fact]
    public void Box_around_a_point_contains_every_point_within_the_radius()
    {
        var box = BoundingBox.Around(32.0853, 34.7818, 10);
        // Points ~9.9 km due north, south, east and west.
        Assert.True(box.Contains(32.0853 + 9.9 / 111.32, 34.7818));
        Assert.True(box.Contains(32.0853 - 9.9 / 111.32, 34.7818));
        Assert.True(box.Contains(32.0853, 34.7818 + 0.105));
        Assert.True(box.Contains(32.0853, 34.7818 - 0.105));
        Assert.False(box.Contains(32.0853 + 11 / 111.32, 34.7818));
    }

    [Fact]
    public void Box_crossing_the_180_meridian_wraps()
    {
        var box = new BoundingBox(South: -10, West: 170, North: 10, East: -170);
        Assert.True(box.Contains(0, 175));
        Assert.True(box.Contains(0, -175));
        Assert.False(box.Contains(0, 0));
    }
}
