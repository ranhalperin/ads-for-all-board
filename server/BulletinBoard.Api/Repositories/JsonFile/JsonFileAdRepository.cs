using System.Diagnostics;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Storage;
using BulletinBoard.Api.Repositories;

namespace BulletinBoard.Api.Repositories.JsonFile;

/// <summary>
/// Ads in one JSON file, for one API server: this process is the only one that reads or writes it.
///
/// Reads:   served from an in-memory snapshot, loaded from ads.json on the first request.
/// Writes:  SemaphoreSlim (one write at a time) → change a copy of the ads → save ads.json (temp + rename)
///          → swap in the new snapshot.
///
/// Must be registered as a singleton: one snapshot and one lock per process.
/// </summary>
public class JsonFileAdRepository(
    IFileStore store,
    StorageOptions options,
    ILogger<JsonFileAdRepository> logger) : IAdRepository
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile JsonFileSnapshot? _snapshot;

    public async Task<AdSearchPage> SearchAsync(AdSearch search, int page, int pageSize, CancellationToken ct)
    {
        var matches = AdFilter.Apply((await GetSnapshotAsync(ct)).Ads, search);
        var items = AdFilter.NewestFirst(matches)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return new AdSearchPage(items, matches.Count);
    }

    public async Task<AdPinsPage> GetPinsAsync(AdSearch search, int maxPins, CancellationToken ct)
    {
        var matches = AdFilter.Apply((await GetSnapshotAsync(ct)).Ads, search);
        // Sorting costs time, so only when we must choose which pins to leave out (newest win).
        IEnumerable<AdMatch> chosen = matches.Count > maxPins
            ? AdFilter.NewestFirst(matches).Take(maxPins)
            : matches;
        return new AdPinsPage(chosen.Select(m => m.Ad).ToList(), matches.Count);
    }

    public async Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetSnapshotAsync(ct)).ById.GetValueOrDefault(id);

    public Task AddAsync(Ad ad, CancellationToken ct) =>
        UpdateJsonFileAsync(ads => ads.Add(ad), ct);

    public Task UpdateAsync(Ad ad, CancellationToken ct) =>
        UpdateJsonFileAsync(ads => ads[IndexOf(ads, ad.Id)] = ad, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct) =>
        UpdateJsonFileAsync(ads => ads.RemoveAt(IndexOf(ads, id)), ct);

    /// <summary>The ads in memory, loading ads.json first if this is the first request.</summary>
    private async Task<JsonFileSnapshot> GetSnapshotAsync(CancellationToken ct) =>
        _snapshot ?? await WithLockAsync(() => LoadAsync(ct), ct);

    /// <summary>Reads ads.json into the snapshot, once. Call only while holding the lock.</summary>
    private async Task<JsonFileSnapshot> LoadAsync(CancellationToken ct)
    {
        // Another request may have loaded it while this one waited for the lock.
        if (_snapshot is not null) return _snapshot;

        var started = Stopwatch.StartNew();
        var ads = await store.ReadAdsAsync(ct);
        logger.LogInformation("Loaded {Count} ads in {Ms} ms", ads.Count, started.ElapsedMilliseconds);
        _snapshot = new JsonFileSnapshot(ads);
        return _snapshot;
    }

    /// <summary>Takes the lock, applies <paramref name="change"/> to a copy of the ads, saves them, then swaps the snapshot.</summary>
    private Task UpdateJsonFileAsync(Action<List<Ad>> change, CancellationToken ct) =>
        WithLockAsync(async () =>
        {
            var ads = new List<Ad>((await LoadAsync(ct)).Ads);
            change(ads); // may throw (not found): nothing has been written yet

            // From here on, finish the write even if the caller goes away, so the file and memory agree.
            await store.WriteAdsAsync(ads, CancellationToken.None);
            _snapshot = new JsonFileSnapshot(ads);
            return true;
        }, ct);

    /// <summary>Runs <paramref name="action"/> holding the lock, giving up with 503 after Storage:LockTimeoutSeconds.</summary>
    private async Task<T> WithLockAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        if (!await _lock.WaitAsync(options.LockTimeout, ct))
            throw new StorageBusyException();
        try
        {
            return await action();
        }
        finally
        {
            _lock.Release();
        }
    }

    private static int IndexOf(List<Ad> ads, Guid id)
    {
        var index = ads.FindIndex(a => a.Id == id);
        return index >= 0 ? index : throw new NotFoundException("Ad not found.");
    }
}
