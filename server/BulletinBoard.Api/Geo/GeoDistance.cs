namespace BulletinBoard.Api.Geo;

public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0088;

    /// <summary>Great-circle distance in km between two points (haversine formula).</summary>
    public static double Km(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}

/// <summary>
/// A lat/lng rectangle. Used as a cheap first check before the exact distance, and for
/// "only what's on the map".
/// </summary>
public readonly record struct BoundingBox(double South, double West, double North, double East)
{
    private const double KmPerDegreeLat = 111.32;

    /// <summary>The smallest box that contains the circle of <paramref name="radiusKm"/> around a point.</summary>
    public static BoundingBox Around(double lat, double lng, double radiusKm)
    {
        var dLat = radiusKm / KmPerDegreeLat;
        var cosLat = Math.Cos(lat * Math.PI / 180);
        // Near the poles a degree of longitude shrinks to nothing: take the full longitude range.
        var dLng = cosLat < 1e-6 ? 180 : Math.Min(180, radiusKm / (KmPerDegreeLat * cosLat));
        return new BoundingBox(lat - dLat, lng - dLng, lat + dLat, lng + dLng);
    }

    public bool Contains(double lat, double lng)
    {
        if (lat < South || lat > North) return false;
        // A box that crosses the 180° meridian has West > East.
        return West <= East ? lng >= West && lng <= East : lng >= West || lng <= East;
    }
}
