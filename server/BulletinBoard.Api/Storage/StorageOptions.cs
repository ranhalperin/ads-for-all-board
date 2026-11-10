namespace BulletinBoard.Api.Storage;

/// <summary>The "Storage" section of appsettings.json.</summary>
public class StorageOptions
{
    /// <summary>Folder that holds ads.json. Relative paths are resolved against the app's content root.</summary>
    public string FolderPath { get; set; } = "data";

    /// <summary>Copied to ads.json the first time the folder is empty. Optional.</summary>
    public string? SeedFile { get; set; } = "SeedData/ads.json";

    /// <summary>How long a request waits for the lock before giving up with 503.</summary>
    public int LockTimeoutSeconds { get; set; } = 15;

    /// <summary>Most pins the map endpoint returns in one response.</summary>
    public int MaxPins { get; set; } = 2_000;

    public TimeSpan LockTimeout => TimeSpan.FromSeconds(LockTimeoutSeconds);
}
