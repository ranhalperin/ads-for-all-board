using System.Diagnostics;
using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Storage;

namespace BulletinBoard.Api.Repositories;

/// <summary>
/// Ads in one JSON file, safe for several servers sharing the file's folder.
///
/// Writes:  local SemaphoreSlim (queues this server's requests) → shared file lock (one server at a time)
///          → re-read if another server wrote → change → save ads.json (temp + rename) → bump ads.version.
/// Reads:   served from an in-memory snapshot. Each read checks ads.version (a few bytes); only when another
///          server has written since does this server reload ads.json.
///
/// Must be registered as a singleton: one snapshot and one local lock per process.
/// </summary>
public sealed class JsonFileAdRepository(
    IFileStore store,
    IDistributedLock sharedLock,
    StorageOptions options,
    ILogger<JsonFileAdRepository> logger) : IAdRepository
{
    private const string LockName = "ads";

    private readonly SemaphoreSlim _localLock = new(1, 1);
    private volatile Snapshot _snapshot = Snapshot.NotLoaded;

    public async Task<IReadOnlyList<Ad>> GetAllAsync(CancellationToken ct) =>
        (await GetSnapshotAsync(ct)).Ads;

    public async Task<Ad?> GetByIdAsync(Guid id, CancellationToken ct) =>
        (await GetSnapshotAsync(ct)).ById.GetValueOrDefault(id);

    public Task AddAsync(Ad ad, CancellationToken ct) =>
        MutateAsync(ads =>
        {
            if (ads.Count >= options.MaxAds)
                throw new StorageFullException($"The board is full ({options.MaxAds:N0} ads).");
            ads.Add(ad);
            return true;
        }, ct);

    public Task<Ad> UpdateAsync(Guid id, Func<Ad, Ad> update, CancellationToken ct) =>
        MutateAsync(ads =>
        {
            var index = IndexOf(ads, id);
            var updated = update(ads[index]);
            ads[index] = updated;
            return updated;
        }, ct);

    public Task DeleteAsync(Guid id, CancellationToken ct) =>
        MutateAsync(ads =>
        {
            var index = IndexOf(ads, id);
            ads.RemoveAt(index);
            return true;
        }, ct);

    /// <summary>The current snapshot, reloading it first if another server has written since.</summary>
    private async Task<Snapshot> GetSnapshotAsync(CancellationToken ct)
    {
        var current = _snapshot;
        var fileVersion = await store.ReadVersionAsync(ct);

        // Up to date, or the version file was caught mid-write (it'll be read again on the next request).
        if (current.IsLoaded && (fileVersion is null || fileVersion == current.Version))
            return current;

        try
        {
            return await ExclusiveAsync(async () =>
            {
                // Another request on this server may have reloaded while we waited for the lock.
                var version = await store.ReadVersionAsync(ct) ?? 0;
                if (_snapshot.IsLoaded && _snapshot.Version == version) return _snapshot;

                var started = Stopwatch.StartNew();
                var ads = await store.ReadAdsAsync(ct);
                logger.LogInformation("Loaded {Count} ads (version {Version}) in {Ms} ms",
                    ads.Count, version, started.ElapsedMilliseconds);
                return _snapshot = new Snapshot(version, ads);
            }, ct);
        }
        catch (StorageBusyException) when (current.IsLoaded)
        {
            // Writers are holding the file: a slightly older list is better than an error for a read.
            logger.LogWarning("Lock busy while reloading; serving version {Version}", current.Version);
            return current;
        }
    }

    private Task<T> MutateAsync<T>(Func<List<Ad>, T> change, CancellationToken ct) =>
        ExclusiveAsync(async () =>
        {
            var version = await store.ReadVersionAsync(ct) ?? 0;

            // Our snapshot is current only if no other server has written since we loaded it.
            var ads = _snapshot.IsLoaded && _snapshot.Version == version
                ? new List<Ad>(_snapshot.Ads)
                : await store.ReadAdsAsync(ct);

            var result = change(ads); // may throw (not found, full): nothing has been written yet

            // From here on, finish the write even if the caller goes away, so the file and version agree.
            var next = version + 1;
            await store.WriteAdsAsync(ads, CancellationToken.None);
            await store.WriteVersionAsync(next, CancellationToken.None);
            _snapshot = new Snapshot(next, ads);
            return result;
        }, ct);

    /// <summary>Runs <paramref name="action"/> holding both locks, giving up after Storage:LockTimeoutSeconds.</summary>
    private async Task<T> ExclusiveAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        var waited = Stopwatch.StartNew();
        if (!await _localLock.WaitAsync(options.LockTimeout, ct))
            throw new StorageBusyException();
        try
        {
            var left = options.LockTimeout - waited.Elapsed;
            await using var _ = await sharedLock.AcquireAsync(LockName, left > TimeSpan.Zero ? left : TimeSpan.Zero, ct);
            return await action();
        }
        finally
        {
            _localLock.Release();
        }
    }

    private static int IndexOf(List<Ad> ads, Guid id)
    {
        var index = ads.FindIndex(a => a.Id == id);
        return index >= 0 ? index : throw new NotFoundException("Ad not found.");
    }
}
