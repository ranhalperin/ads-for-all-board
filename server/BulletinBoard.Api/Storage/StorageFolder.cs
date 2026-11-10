using IOPath = System.IO.Path;

namespace BulletinBoard.Api.Storage;

/// <summary>The folder that holds the store's one file, ads.json.</summary>
public class StorageFolder(string path)
{
    // Built once: the folder never changes.
    private readonly string _adsFile = IOPath.Combine(path, "ads.json");

    /// <summary>The folder. <see cref="Resolve"/> makes it a full path.</summary>
    public string GetPath { get; } = string.IsNullOrWhiteSpace(path)
        ? throw new ArgumentException("A storage folder is required.", nameof(path))
        : path;

    /// <summary>The ads themselves (ads.json).</summary>
    public string GetAdsFile() => _adsFile;

    /// <summary>A relative folder path is taken from <paramref name="contentRoot"/> (the API's folder).</summary>
    public static StorageFolder Resolve(string folderPath, string contentRoot) =>
        new(IOPath.GetFullPath(IOPath.IsPathRooted(folderPath)
            ? folderPath
            : IOPath.Combine(contentRoot, folderPath)));
}
