namespace BulletinBoard.Api.Storage;

/// <summary>The "Storage" section of appsettings.json.</summary>
public class StorageOptions
{
    /// <summary>
    /// Folder that holds ads.json, ads.version and ads.lock. Relative paths are resolved against the
    /// app's content root. In the cloud, point it at the shared mount (EFS, Azure Files, an NFS volume).
    /// </summary>
    public string FolderPath { get; set; } = "data";

    /// <summary>Copied to ads.json the first time the folder is empty. Optional.</summary>
    public string? SeedFile { get; set; } = "SeedData/ads.json";

    /// <summary>How long a write waits for the lock before giving up with 503.</summary>
    public int LockTimeoutSeconds { get; set; } = 15;

    /// <summary>Random pause between attempts to take the shared file lock (jitter avoids servers retrying in step).</summary>
    public int LockRetryMinMs { get; set; } = 25;
    public int LockRetryMaxMs { get; set; } = 100;

    /// <summary>Creating an ad above this count is refused (the single-file design is sized for 100K).</summary>
    public int MaxAds { get; set; } = 100_000;

    /// <summary>Most pins the map endpoint returns in one response.</summary>
    public int MaxPins { get; set; } = 2_000;

    public TimeSpan LockTimeout => TimeSpan.FromSeconds(LockTimeoutSeconds);
}

/// <summary>The three files of the store, all in one folder.</summary>
public sealed record StorageFolder(string Path)
{
    public string AdsFile => System.IO.Path.Combine(Path, "ads.json");
    public string VersionFile => System.IO.Path.Combine(Path, "ads.version");
    public string LockFile(string name) => System.IO.Path.Combine(Path, name + ".lock");

    public static StorageFolder Resolve(string folderPath, string contentRoot) =>
        new(System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(folderPath)
            ? folderPath
            : System.IO.Path.Combine(contentRoot, folderPath)));
}
