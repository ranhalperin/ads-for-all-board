using BulletinBoard.Api.Models;
using BulletinBoard.Api.Repositories;
using BulletinBoard.Api.Repositories.JsonFile;
using BulletinBoard.Api.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace BulletinBoard.Api.Tests;

/// <summary>A throwaway store folder. Each repository made from it acts like a separate server.</summary>
public sealed class TempStore : IDisposable
{
    public StorageFolder Folder { get; } =
        new(Path.Combine(Path.GetTempPath(), "bulletin-board-tests", Guid.NewGuid().ToString("N")));

    public StorageOptions Options { get; } = new() { LockTimeoutSeconds = 10, LockRetryMinMs = 2, LockRetryMaxMs = 15 };

    /// <summary>A new repository with its own memory and local lock: "another server" on the same folder.</summary>
    public JsonFileAdRepository NewServer() => new(
        new SharedFolderFileStore(Folder),
        new SharedFileLock(Folder, Options.LockRetryMinMs, Options.LockRetryMaxMs),
        Options,
        NullLogger<JsonFileAdRepository>.Instance);

    public void Dispose()
    {
        try { Directory.Delete(Folder.GetPath, recursive: true); } catch (IOException) { }
    }
}

public static class Ads
{
    public static Ad Make(
        string title = "Test ad",
        Category category = Category.ForSale,
        decimal? price = 100,
        double lat = 32.0853,
        double lng = 34.7818,
        string contactName = "Dana",
        DateTime? created = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "Description",
        Category = category,
        Price = price,
        ContactName = contactName,
        ContactPhone = "050-1234567",
        Latitude = lat,
        Longitude = lng,
        CreatedAt = created ?? DateTime.UtcNow,
    };
}

public static class RepositoryTestExtensions
{
    /// <summary>Every stored ad (newest first): a search with no criteria and one page big enough for all.</summary>
    public static async Task<IReadOnlyList<Ad>> AllAsync(this IAdRepository repository) =>
        (await repository.SearchAsync(new AdSearch(), 1, int.MaxValue, default)).Items.Select(m => m.Ad).ToList();
}
