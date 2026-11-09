// Generates N fake ads into a store folder, to check the API at scale (default 100,000).
// Uses the API's own file store and shared lock, so it's safe to run while servers are up:
// they see the new ads.version and reload on their next request.
//
//   dotnet run --project tools/BulletinBoard.DataGen -- [count] [folder]
//   dotnet run --project tools/BulletinBoard.DataGen -- 100000 server/BulletinBoard.Api/data

using System.Diagnostics;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Storage;

var count = args.Length > 0 ? int.Parse(args[0]) : 100_000;
var folder = StorageFolder.Resolve(args.Length > 1 ? args[1] : "server/BulletinBoard.Api/data", Environment.CurrentDirectory);

var random = new Random(42);
(string Name, double Lat, double Lng)[] cities =
[
    ("Tel Aviv", 32.0853, 34.7818), ("Jerusalem", 31.7683, 35.2137), ("Haifa", 32.7940, 34.9896),
    ("Beersheba", 31.2520, 34.7915), ("Netanya", 32.3215, 34.8532), ("Ashdod", 31.8044, 34.6553),
    ("Rishon LeZion", 31.9730, 34.7925), ("Petah Tikva", 32.0840, 34.8878), ("Eilat", 29.5577, 34.9519),
    ("Tiberias", 32.7922, 35.5312),
];
string[] items = ["bike", "sofa", "laptop", "phone", "table", "guitar", "stroller", "camera", "washing machine", "fridge", "apartment", "car", "scooter", "tutor", "plumber"];
string[] adjectives = ["Used", "Like new", "Cheap", "Great", "Vintage", "Small", "Large", "Modern"];
string[] names = ["Dana", "Yossi", "Noa", "Avi", "Michal", "Eli", "Tamar", "Gil", "Lior", "Ronit"];
var categories = Enum.GetValues<Category>();
var now = DateTime.UtcNow;

var ads = new List<Ad>(count);
for (var i = 0; i < count; i++)
{
    var city = cities[random.Next(cities.Length)];
    var category = categories[random.Next(categories.Length)];
    ads.Add(new Ad
    {
        Id = Guid.NewGuid(),
        Title = $"{adjectives[random.Next(adjectives.Length)]} {items[random.Next(items.Length)]} #{i + 1}",
        Description = string.Join(' ', Enumerable.Repeat("Good condition, pick up in person, price is negotiable.", 1 + random.Next(6))),
        Category = category,
        Price = category is Category.Jobs or Category.Wanted ? null : random.Next(0, 20_000),
        ContactName = names[random.Next(names.Length)],
        ContactPhone = $"05{random.Next(0, 9)}-{random.Next(1_000_000, 9_999_999)}",
        // Spread up to ~15 km around the city centre.
        Latitude = Math.Round(city.Lat + (random.NextDouble() - 0.5) * 0.27, 5),
        Longitude = Math.Round(city.Lng + (random.NextDouble() - 0.5) * 0.32, 5),
        Address = city.Name,
        CreatedAt = now.AddMinutes(-random.Next(0, 60 * 24 * 90)),
    });
}

var store = new SharedFolderFileStore(folder);
var fileLock = new SharedFileLock(folder);
var timer = Stopwatch.StartNew();
await using (await fileLock.LockShareAdsFile("ads", TimeSpan.FromSeconds(30), CancellationToken.None))
{
    var version = await store.ReadVersionAsync(CancellationToken.None) ?? 0;
    await store.WriteAdsAsync(ads, CancellationToken.None);
    await store.WriteVersionAsync(version + 1, CancellationToken.None);
}

var size = new FileInfo(folder.GetAdsFile()).Length;
Console.WriteLine($"Wrote {count:N0} ads to {folder.GetAdsFile()} ({size / 1024.0 / 1024.0:N1} MB) in {timer.ElapsedMilliseconds:N0} ms");
