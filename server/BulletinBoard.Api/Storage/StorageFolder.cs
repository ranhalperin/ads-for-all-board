using IOPath = System.IO.Path;

namespace BulletinBoard.Api.Storage;

/// <summary>The three files of the store (ads.json, ads.version, ads.lock), all in one folder.</summary>
public class StorageFolder(string path)
{
    /// <summary>The folder: a local folder, or the shared mount in the cloud. <see cref="Resolve"/> makes it a full path.</summary>
    public string Path { get; } = string.IsNullOrWhiteSpace(path)
        ? throw new ArgumentException("A storage folder is required.", nameof(path))
        : path;

    // Built once: the folder never changes.
    private readonly string _adsFile = IOPath.Combine(path, "ads.json");
    private readonly string _versionFile = IOPath.Combine(path, "ads.version");

    /// <summary>The ads themselves (ads.json).</summary>
    public string GetAdsFile() => _adsFile;

    /// <summary>A counter, bumped on every write, so each server knows when to reload (ads.version).</summary>
    public string GetVersionFile() => _versionFile;

    /// <summary>The file held open exclusively while one server writes (for example "ads" → ads.lock).</summary>
    public string GetLockFile(string name) => IOPath.Combine(Path, name + ".lock");

    /// <summary>A relative folder path is taken from <paramref name="contentRoot"/> (the API's folder).</summary>
    public static StorageFolder Resolve(string folderPath, string contentRoot) =>
        new(IOPath.GetFullPath(IOPath.IsPathRooted(folderPath)
            ? folderPath
            : IOPath.Combine(contentRoot, folderPath)));
}
