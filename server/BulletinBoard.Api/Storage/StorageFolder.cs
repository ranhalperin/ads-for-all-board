namespace BulletinBoard.Api.Storage;

/// <summary>The three files of the store (ads.json, ads.version, ads.lock), all in one folder.</summary>
public class StorageFolder
{
    public StorageFolder(string path)
    {
        Path = path;
    }

    /// <summary>The folder's full path: a local folder, or the shared mount in the cloud.</summary>
    public string Path { get; }

    /// <summary>The ads themselves.</summary>
    public string AdsFile => System.IO.Path.Combine(Path, "ads.json");

    /// <summary>A counter, bumped on every write, so each server knows when to reload.</summary>
    public string VersionFile => System.IO.Path.Combine(Path, "ads.version");

    /// <summary>The file held open exclusively while one server writes (for example "ads" → ads.lock).</summary>
    public string LockFile(string name) => System.IO.Path.Combine(Path, name + ".lock");

    /// <summary>A relative folder path is taken from <paramref name="contentRoot"/> (the API's folder).</summary>
    public static StorageFolder Resolve(string folderPath, string contentRoot) =>
        new(System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(folderPath)
            ? folderPath
            : System.IO.Path.Combine(contentRoot, folderPath)));
}
