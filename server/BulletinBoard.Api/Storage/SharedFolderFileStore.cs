using System.Text.Json;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Storage;

/// <summary>ads.json in one folder, created from the seed file the first time.</summary>
public sealed class SharedFolderFileStore : IFileStore
{
    private const int BufferSize = 64 * 1024;
    private readonly StorageFolder _folder;

    public SharedFolderFileStore(StorageFolder folder, string? seedFile = null)
    {
        _folder = folder;
        EnsureFiles(seedFile);
    }

    public async Task<List<Ad>> ReadAdsAsync(CancellationToken ct)
    {
        await using var stream = new FileStream(_folder.GetAdsFile(), FileMode.Open, FileAccess.Read,
            FileShare.Read, BufferSize, useAsync: true);
        return await JsonSerializer.DeserializeAsync<List<Ad>>(stream, JsonDefaults.Options, ct) ?? [];
    }

    public async Task WriteAdsAsync(IReadOnlyCollection<Ad> ads, CancellationToken ct)
    {
        var temp = _folder.GetAdsFile() + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write,
                             FileShare.None, BufferSize, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(stream, ads, JsonDefaults.Options, ct);
                await stream.FlushAsync(ct);
                stream.Flush(flushToDisk: true);
            }
            await MoveWithRetryAsync(temp, _folder.GetAdsFile(), ct);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private void EnsureFiles(string? seedFile)
    {
        Directory.CreateDirectory(_folder.GetPath);
        if (File.Exists(_folder.GetAdsFile())) return;

        if (seedFile is not null && File.Exists(seedFile))
            File.Copy(seedFile, _folder.GetAdsFile());
        else
            File.WriteAllText(_folder.GetAdsFile(), "[]");
    }

    /// <summary>
    /// Renames the temp file over ads.json. On Windows a virus scanner or backup tool can hold the
    /// target for a moment, so a few short retries.
    /// </summary>
    private static async Task MoveWithRetryAsync(string from, string to, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(from, to, overwrite: true);
                return;
            }
            catch (Exception e) when (attempt < 5 && e is IOException or UnauthorizedAccessException)
            {
                await Task.Delay(50 * attempt, ct);
            }
        }
    }
}
