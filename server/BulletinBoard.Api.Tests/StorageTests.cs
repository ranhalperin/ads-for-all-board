using BulletinBoard.Api.Errors;
using BulletinBoard.Api.Storage;

namespace BulletinBoard.Api.Tests;

public class StorageTests : IDisposable
{
    private readonly TempStore _store = new();

    public void Dispose() => _store.Dispose();

    [Fact]
    public async Task Empty_folder_starts_with_no_ads_and_version_0()
    {
        var server = _store.NewServer();
        Assert.Empty(await server.AllAsync());
        Assert.True(File.Exists(_store.Folder.GetAdsFile()));
        Assert.Equal("0", await File.ReadAllTextAsync(_store.Folder.GetVersionFile()));
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
    public async Task Each_write_bumps_the_version_and_leaves_no_temp_files()
    {
        var server = _store.NewServer();
        var ad = Ads.Make();
        await server.AddAsync(ad, default);
        await server.UpdateAsync(ad with { Title = "Changed" }, default);
        await server.DeleteAsync(ad.Id, default);

        Assert.Equal("3", await File.ReadAllTextAsync(_store.Folder.GetVersionFile()));
        Assert.Empty(Directory.GetFiles(_store.Folder.GetPath, "*.tmp"));
    }

    [Fact]
    public async Task Server_2_sees_a_write_made_by_server_1_on_its_next_read()
    {
        var server1 = _store.NewServer();
        var server2 = _store.NewServer();
        Assert.Empty(await server2.AllAsync()); // server 2 has loaded and cached the empty list

        var ad = Ads.Make("Posted on server 1");
        await server1.AddAsync(ad, default);

        var seen = Assert.Single(await server2.AllAsync());
        Assert.Equal("Posted on server 1", seen.Title);
        Assert.NotNull(await server2.GetByIdAsync(ad.Id, default));
    }

    [Fact]
    public async Task Write_on_a_stale_server_does_not_lose_the_other_servers_ad()
    {
        var server1 = _store.NewServer();
        var server2 = _store.NewServer();
        await server2.AllAsync(); // cache the empty list

        await server1.AddAsync(Ads.Make("From server 1"), default);
        await server2.AddAsync(Ads.Make("From server 2"), default); // its cache is stale: must re-read first

        var titles = (await _store.NewServer().AllAsync()).Select(a => a.Title).Order();
        Assert.Equal(["From server 1", "From server 2"], titles);
    }

    [Fact]
    public async Task Concurrent_writes_from_two_servers_are_all_kept()
    {
        var server1 = _store.NewServer();
        var server2 = _store.NewServer();
        const int perServer = 25;

        var writes = Enumerable.Range(0, perServer).SelectMany(i => new[]
        {
            Task.Run(() => server1.AddAsync(Ads.Make($"s1-{i}"), default)),
            Task.Run(() => server2.AddAsync(Ads.Make($"s2-{i}"), default)),
        });
        await Task.WhenAll(writes);

        var all = await _store.NewServer().AllAsync();
        Assert.Equal(2 * perServer, all.Count);
        Assert.Equal(2 * perServer, all.Select(a => a.Id).Distinct().Count());
        Assert.Equal((2 * perServer).ToString(), await File.ReadAllTextAsync(_store.Folder.GetVersionFile()));
    }

    [Fact]
    public async Task Refused_change_writes_nothing()
    {
        var server = _store.NewServer();
        var ad = Ads.Make();
        await server.AddAsync(ad, default);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            server.UpdateAsync(Ads.Make(), default)); // an id that isn't stored
        await Assert.ThrowsAsync<NotFoundException>(() =>
            server.DeleteAsync(Guid.NewGuid(), default));

        Assert.Equal("1", await File.ReadAllTextAsync(_store.Folder.GetVersionFile()));
    }

    [Fact]
    public async Task Shared_lock_is_exclusive_and_times_out()
    {
        var lockA = new SharedFileLock(_store.Folder, 2, 10);
        var lockB = new SharedFileLock(_store.Folder, 2, 10);
        Directory.CreateDirectory(_store.Folder.GetPath);

        var held = await lockA.AcquireAsync("ads", TimeSpan.FromSeconds(1), default);
        await Assert.ThrowsAsync<StorageBusyException>(() =>
            lockB.AcquireAsync("ads", TimeSpan.FromMilliseconds(200), default));

        await held.DisposeAsync();
        await using var second = await lockB.AcquireAsync("ads", TimeSpan.FromSeconds(1), default);
    }

    [Fact]
    public async Task Waiting_writer_gets_the_lock_once_it_is_released()
    {
        var server = _store.NewServer();
        var outside = new SharedFileLock(_store.Folder, 2, 10);
        var held = await outside.AcquireAsync("ads", TimeSpan.FromSeconds(1), default);

        var write = server.AddAsync(Ads.Make("Waited"), default);
        await Task.Delay(150);
        Assert.False(write.IsCompleted); // blocked on the shared lock

        await held.DisposeAsync();
        await write;
        Assert.Single(await server.AllAsync());
    }
}
