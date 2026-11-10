using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Models;
using BulletinBoard.Api.Storage;

namespace BulletinBoard.Api.Tests;

public class StorageTests : IDisposable
{
    private readonly TempStore _store = new();

    public void Dispose() => _store.Dispose();

    [Fact]
    public async Task Empty_folder_starts_with_no_ads_and_only_ads_json()
    {
        var server = _store.NewServer();
        Assert.Empty(await server.AllAsync());
        Assert.Equal(["ads.json"], Directory.GetFiles(_store.Folder.GetPath).Select(Path.GetFileName));
    }

    [Fact]
    public async Task Ads_survive_a_restart()
    {
        var ad = Ads.Make("Saved bike");
        await _store.NewServer().AddAsync(ad, default);

        var restarted = _store.NewServer();
        var loaded = Assert.Single(await restarted.AllAsync());
        Assert.Equal(ad, loaded);
    }

    [Fact]
    public async Task Each_write_is_saved_and_leaves_no_temp_files()
    {
        var server = _store.NewServer();
        var kept = Ads.Make("Kept");
        var removed = Ads.Make("Removed");
        await server.AddAsync(kept, default);
        await server.AddAsync(removed, default);
        await server.UpdateAsync(kept with { Title = "Changed" }, default);
        await server.DeleteAsync(removed.Id, default);

        var saved = Assert.Single(await _store.NewServer().AllAsync());
        Assert.Equal("Changed", saved.Title);
        Assert.Empty(Directory.GetFiles(_store.Folder.GetPath, "*.tmp"));
    }

    [Fact]
    public async Task Concurrent_writes_are_all_kept()
    {
        var server = _store.NewServer();
        const int writes = 50;

        await Task.WhenAll(Enumerable.Range(0, writes)
            .Select(i => Task.Run(() => server.AddAsync(Ads.Make($"ad-{i}"), default))));

        Assert.Equal(writes, (await server.AllAsync()).Count);
        var saved = await _store.NewServer().AllAsync();
        Assert.Equal(writes, saved.Count);
        Assert.Equal(writes, saved.Select(a => a.Id).Distinct().Count());
    }

    [Fact]
    public async Task Refused_change_writes_nothing()
    {
        var server = _store.NewServer();
        await server.AddAsync(Ads.Make(), default);
        var before = await File.ReadAllTextAsync(_store.Folder.GetAdsFile());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            server.UpdateAsync(Ads.Make(), default)); // an id that isn't stored
        await Assert.ThrowsAsync<NotFoundException>(() =>
            server.DeleteAsync(Guid.NewGuid(), default));

        Assert.Equal(before, await File.ReadAllTextAsync(_store.Folder.GetAdsFile()));
    }

    [Fact]
    public async Task Second_writer_waits_for_the_first()
    {
        var gate = new GatedStore(new SharedFolderFileStore(_store.Folder));
        var server = _store.NewServer(gate);
        await server.AllAsync();

        gate.Hold();
        var first = server.AddAsync(Ads.Make("First"), default);
        var second = server.AddAsync(Ads.Make("Second"), default);
        await Task.Delay(150);
        Assert.False(first.IsCompleted);  // held inside the save
        Assert.False(second.IsCompleted); // waiting for the lock

        gate.Release();
        await Task.WhenAll(first, second);
        Assert.Equal(2, (await _store.NewServer().AllAsync()).Count);
    }

    [Fact]
    public async Task Writer_gives_up_with_busy_after_the_lock_timeout()
    {
        var gate = new GatedStore(new SharedFolderFileStore(_store.Folder));
        var server = _store.NewServer(gate, new StorageOptions { LockTimeoutSeconds = 1 });
        await server.AllAsync();

        gate.Hold();
        var first = server.AddAsync(Ads.Make("First"), default);
        await Assert.ThrowsAsync<StorageBusyException>(() => server.AddAsync(Ads.Make("Too late"), default));

        gate.Release();
        await first;
        Assert.Equal("First", Assert.Single(await server.AllAsync()).Title);
    }

    /// <summary>A file store whose saves can be held open, to keep the repository's lock taken.</summary>
    private sealed class GatedStore(IFileStore inner) : IFileStore
    {
        private TaskCompletionSource _open = Open();

        public void Hold() => _open = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Release() => _open.TrySetResult();

        public Task<List<Ad>> ReadAdsAsync(CancellationToken ct) => inner.ReadAdsAsync(ct);

        public async Task WriteAdsAsync(IReadOnlyCollection<Ad> ads, CancellationToken ct)
        {
            await _open.Task;
            await inner.WriteAdsAsync(ads, ct);
        }

        private static TaskCompletionSource Open()
        {
            var open = new TaskCompletionSource();
            open.SetResult();
            return open;
        }
    }
}
