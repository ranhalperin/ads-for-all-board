using System.Text;
using System.Text.Json;
using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Storage;

/// <summary>
/// ads.json and ads.version in one folder: a local folder in development, or a shared network
/// folder (NFS / SMB) that every server mounts in the cloud.
/// </summary>
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

    public async Task<long?> ReadVersionAsync(CancellationToken ct)
    {
        try
        {
            // Opening the file (not just checking its timestamp) is what makes NFS return fresh
            // contents: close-to-open consistency. Share flags let a writer replace it meanwhile.
            await using var stream = new FileStream(_folder.GetVersionFile(), FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 64, useAsync: true);
            using var reader = new StreamReader(stream, Encoding.ASCII);
            var text = await reader.ReadToEndAsync(ct);
            return long.TryParse(text.Trim(), out var version) ? version : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public async Task WriteVersionAsync(long version, CancellationToken ct)
    {
        await using var stream = new FileStream(_folder.GetVersionFile(), FileMode.OpenOrCreate, FileAccess.Write,
            FileShare.ReadWrite | FileShare.Delete, 64, useAsync: true);
        var bytes = Encoding.ASCII.GetBytes(version.ToString());
        stream.SetLength(0);
        await stream.WriteAsync(bytes, ct);
        await stream.FlushAsync(ct);
        stream.Flush(flushToDisk: true);
    }

    private void EnsureFiles(string? seedFile)
    {
        Directory.CreateDirectory(_folder.GetPath);
        if (!File.Exists(_folder.GetAdsFile()))
        {
            try
            {
                if (seedFile is not null && File.Exists(seedFile))
                    File.Copy(seedFile, _folder.GetAdsFile(), overwrite: false);
                else
                    File.WriteAllText(_folder.GetAdsFile(), "[]");
            }
            catch (IOException) when (File.Exists(_folder.GetAdsFile()))
            {
                // Another server created it first.
            }
        }

        if (!File.Exists(_folder.GetVersionFile()))
        {
            try
            {
                using var stream = new FileStream(_folder.GetVersionFile(), FileMode.CreateNew, FileAccess.Write);
                stream.Write("0"u8);
            }
            catch (IOException) when (File.Exists(_folder.GetVersionFile()))
            {
            }
        }
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
