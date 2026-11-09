namespace BulletinBoard.Api.Storage;

/// <summary>
/// A lock every server sees. Dispose the result to release it.
/// Throws <see cref="Errors.StorageBusyException"/> when it can't be taken within the timeout.
/// </summary>
public interface IDistributedLock
{
    Task<IAsyncDisposable> AtomicLockBetweenServers(string name, TimeSpan timeout, CancellationToken ct);
}
